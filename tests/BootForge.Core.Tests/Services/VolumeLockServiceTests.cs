using BootForge.Core.Interfaces;
using BootForge.DeviceManagement.Native;
using BootForge.DeviceManagement.Services;

namespace BootForge.Core.Tests.Services;

public sealed class VolumeLockServiceTests
{
    [Fact]
    public void AcquireForDisk_LocksAndDismountsOnlyTargetVolumes()
    {
        FakeVolumeHandle firstTarget = new(
            "Target 1",
            [2]);

        FakeVolumeHandle unrelated = new(
            "Other",
            [0]);

        FakeVolumeHandle secondTarget = new(
            "Target 2",
            [2]);

        VolumeLockService service = new(
            new FakeVolumeProvider(
                firstTarget,
                unrelated,
                secondTarget));

        using IVolumeLock volumeLock =
            service.AcquireForDisk(2);

        Assert.Equal(2, volumeLock.VolumeCount);
        Assert.True(firstTarget.IsLocked);
        Assert.True(firstTarget.IsDismounted);
        Assert.True(secondTarget.IsLocked);
        Assert.True(secondTarget.IsDismounted);
        Assert.True(unrelated.IsDisposed);
        Assert.False(unrelated.IsLocked);
    }

    [Fact]
    public void AcquireForDisk_LockFailureReleasesAllHandles()
    {
        FakeVolumeHandle first = new("First", [2]);
        FakeVolumeHandle failing = new(
            "Failing",
            [2])
        {
            FailOnLock = true
        };

        VolumeLockService service = new(
            new FakeVolumeProvider(first, failing));

        Assert.Throws<InvalidOperationException>(
            () => service.AcquireForDisk(2));

        Assert.True(first.IsDisposed);
        Assert.True(failing.IsDisposed);
    }

    [Fact]
    public void AcquireForDisk_DismountFailureReleasesAllHandles()
    {
        FakeVolumeHandle first = new("First", [2]);
        FakeVolumeHandle failing = new(
            "Failing",
            [2])
        {
            FailOnDismount = true
        };

        VolumeLockService service = new(
            new FakeVolumeProvider(first, failing));

        Assert.Throws<InvalidOperationException>(
            () => service.AcquireForDisk(2));

        Assert.True(first.IsDisposed);
        Assert.True(failing.IsDisposed);
    }

    [Fact]
    public void Dispose_ReleasesAllTargetHandles()
    {
        FakeVolumeHandle first = new("First", [2]);
        FakeVolumeHandle second = new("Second", [2]);

        VolumeLockService service = new(
            new FakeVolumeProvider(first, second));

        IVolumeLock volumeLock =
            service.AcquireForDisk(2);

        volumeLock.Dispose();
        volumeLock.Dispose();

        Assert.True(first.IsDisposed);
        Assert.True(second.IsDisposed);
        Assert.Equal(1, first.DisposeCount);
        Assert.Equal(1, second.DisposeCount);
    }

    private sealed class FakeVolumeProvider(
        params IVolumeHandle[] volumes)
        : IVolumeProvider
    {
        public IReadOnlyList<IVolumeHandle> OpenVolumes()
        {
            return volumes;
        }
    }

    private sealed class FakeVolumeHandle(
        string name,
        params int[] diskNumbers)
        : IVolumeHandle
    {
        public string Name { get; } = name;

        public IReadOnlySet<int> DiskNumbers { get; } =
            new HashSet<int>(diskNumbers);

        public bool FailOnLock { get; init; }

        public bool FailOnDismount { get; init; }

        public bool IsLocked { get; private set; }

        public bool IsDismounted { get; private set; }

        public bool IsDisposed { get; private set; }

        public int DisposeCount { get; private set; }

        public void Lock()
        {
            if (FailOnLock)
            {
                throw new InvalidOperationException(
                    "Lock failed.");
            }

            IsLocked = true;
        }

        public void Dismount()
        {
            if (!IsLocked)
            {
                throw new InvalidOperationException(
                    "Not locked.");
            }

            if (FailOnDismount)
            {
                throw new InvalidOperationException(
                    "Dismount failed.");
            }

            IsDismounted = true;
        }

        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            IsDisposed = true;
            DisposeCount++;
        }
    }
}

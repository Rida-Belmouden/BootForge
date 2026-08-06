# Image compatibility analysis

BootForge analyzes a selected image before enabling raw disk writing. The
analysis is conservative: an unrecognized or incomplete boot structure is
blocked rather than treated as bootable.

## ISO images

For ISO images, BootForge reads ISO 9660 volume descriptors and validates the
El Torito boot catalog. Boot catalog platform entries determine whether the
image supports BIOS, UEFI, or both.

An ISO is classified as hybrid when it also contains a valid MBR or GPT layout.
A hybrid image can be written directly to a USB drive while preserving its
embedded disk layout.

## Raw disk images

BootForge recognizes:

- MBR images containing a legacy partition
- GPT images containing a structurally valid primary GPT header with a valid
  header CRC
- UEFI support only when the GPT partition entries contain an EFI System
  Partition

A protective MBR entry with type `0xEE` is not counted as BIOS support. It is
part of the GPT layout and prevents older tools from treating the disk as
unpartitioned.

## Partition and file-system handling

The current writer operates in raw-image mode. It does not create or convert a
partition scheme and does not format the target as FAT32 or NTFS. Every byte of
the image layout and its existing file systems is preserved on the target.

Selectable partition schemes and file-system formatting belong to a future ISO
extraction mode. Until that mode exists, the interface deliberately reports
the file system as preserved from the image.

## References

- [UEFI GPT disk layout](https://uefi.org/specs/UEFI/2.10/05_GUID_Partition_Table_Format.html)
- [UEFI removable media formats](https://uefi.org/specs/UEFI/2.10/13_Protocols_Media_Access.html)
- [Windows file-system comparison](https://learn.microsoft.com/windows/win32/fileio/filesystem-functionality-comparison)

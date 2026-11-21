// ChdCdReader — CD/GD-ROM reader over CHDSharp.
// Behavior parity with RALibretro RAHasher's HashCHD.cpp (GPL, used as
// reference only — this is a new implementation): metadata -> track table,
// sector/frame offset accumulation (incl. the 4-frame padding quirk), the
// sector-16 format probe, and the per-track hunk cache. Tags are matched
// through CHDSharp's string form of the binary tags (CHT2/CHT1/CHGD/DVD ).

using System.Text;
using CHDSharp;
using CHDSharp.Models;
using RetroAchievementsSharp.Models;

namespace RetroAchievementsSharp;

/// <summary>ChdCdReader — CD/GD-ROM reader over CHDSharp. Behavior parity with RALibretro RAHasher's HashCHD.cpp (GPL, used as reference only — this is a new implementation</summary>
public static class ChdCdReader
{
    /* struct metadata_t in HashCHD.cpp */
    private sealed class ChdTrackMetadata
    {
        internal uint Frames;
        internal uint Pregap;
        internal uint Postgap;
        internal uint Track;
        internal uint SectorOffset;
        internal uint FrameOffset;
        internal string Type = "";
    }

    /* struct chd_track_handle_t in HashCHD.cpp */
    private sealed class ChdTrackHandle
    {
        internal ChdFile File = null!;
        internal byte[] HunkMem = [];
        internal uint HunkNum = uint.MaxValue;
        internal uint FramesPerHunk;
        internal uint FirstSector;
        internal uint FirstFrame;
        internal uint FramesInTrack;
        internal uint SectorDataSize;
        internal uint SectorHeaderSize;
    }

    private static readonly byte[] SyncPattern =
    [
        0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00
    ];

    /* chd_error_string equivalent (libchdr names) */
    private static string ChdErrorString(ChdError err)
    {
        var name = err.ToString();
        if (name.StartsWith("Chderr", StringComparison.Ordinal))
        {
            name = name.Substring("Chderr".Length);
        }

        var sb = new StringBuilder("CHDERR_");
        foreach (var c in name)
        {
            if (char.IsUpper(c) && sb.Length > 7)
                sb.Append('_');
            sb.Append(char.ToUpperInvariant(c));
        }

        return sb.ToString();
    }

    private static bool Matches(byte[] buffer, int offset, string pattern, int length)
    {
        for (var i = 0; i < length; ++i)
        {
            if (buffer[offset + i] != (byte)pattern[i])
                return false;
        }

        return true;
    }

    private static bool Matches(byte[] buffer, int offset, byte[] pattern, int length)
    {
        for (var i = 0; i < length; ++i)
        {
            if (buffer[offset + i] != pattern[i])
                return false;
        }

        return true;
    }

    /* the idx-th metadata entry with the given tag, or null */
    private static string? GetMetadataText(ChdFile file, string tag, uint idx)
    {
        uint count = 0;
        foreach (ChdMetadataEntry entry in file.Metadata)
        {
            if (!string.Equals(entry.Tag, tag, StringComparison.Ordinal))
                continue;

            if (count == idx)
            {
                /* the C copies into a 256-byte buffer (incl. NUL); entries are
                 * NUL-terminated text */
                var length = entry.Data.Length;
                if (length > 255)
                {
                    length = 255;
                }

                return Encoding.ASCII.GetString(entry.Data, 0, length);
            }

            ++count;
        }

        return null;
    }

    private static bool StartsWithAt(string text, int pos, string label)
    {
        if (pos + label.Length > text.Length)
            return false;

        for (var i = 0; i < label.Length; ++i)
        {
            if (text[pos + i] != label[i])
                return false;
        }

        return true;
    }

    /* sscanf %u after a literal label (leading whitespace is skipped, like %u) */
    private static uint ParseUnsignedAfter(string text, ref int pos, string label)
    {
        while (pos < text.Length && text[pos] == ' ')
        {
            ++pos;
        }

        if (!StartsWithAt(text, pos, label))
            return 0;

        pos += label.Length;
        while (pos < text.Length && char.IsWhiteSpace(text[pos]))
        {
            ++pos;
        }

        uint value = 0;
        while (pos < text.Length && text[pos] >= '0' && text[pos] <= '9')
        {
            value = value * 10 + (uint)(text[pos] - '0');
            ++pos;
        }

        return value;
    }

    /* sscanf %[^ ] after a literal label (leading whitespace is skipped) */
    private static string ParseStringAfter(string text, ref int pos, string label)
    {
        while (pos < text.Length && text[pos] == ' ')
        {
            ++pos;
        }

        if (!StartsWithAt(text, pos, label))
            return "";

        pos += label.Length;
        var start = pos;
        while (pos < text.Length && text[pos] != ' ')
        {
            ++pos;
        }

        return text.Substring(start, pos - start);
    }

    /* sscanf(meta, CDROM_TRACK_METADATA2_FORMAT, ...) — the format is a
     * superset of the legacy CHT1 (no PREGAP chain) and GDROM (PAD first)
     * formats; fields absent from the string are left untouched (the C reuses
     * the same struct across entries) */
    private static void ParseTrackMetadata(string text, ChdTrackMetadata metadata)
    {
        var pos = 0;

        metadata.Track = ParseUnsignedAfter(text, ref pos, "TRACK:");
        metadata.Type = ParseStringAfter(text, ref pos, "TYPE:");
        _ = ParseStringAfter(text, ref pos, "SUBTYPE:");
        metadata.Frames = ParseUnsignedAfter(text, ref pos, "FRAMES:");

        while (pos < text.Length && text[pos] == ' ')
        {
            ++pos;
        }

        if (StartsWithAt(text, pos, "PAD:"))
        {
            _ = ParseUnsignedAfter(text, ref pos, "PAD:");
            while (pos < text.Length && text[pos] == ' ')
            {
                ++pos;
            }
        }

        if (StartsWithAt(text, pos, "PREGAP:"))
        {
            metadata.Pregap = ParseUnsignedAfter(text, ref pos, "PREGAP:");
            _ = ParseStringAfter(text, ref pos, "PGTYPE:");
            _ = ParseStringAfter(text, ref pos, "PGSUB:");
            metadata.Postgap = ParseUnsignedAfter(text, ref pos, "POSTGAP:");
        }
    }

    /* rc_hash_get_chd_metadata — metadata -> track entry (CHT2, CHT1, CHGD,
     * or a faked MODE1 track for DVD) */
    private static bool GetChdMetadata(ChdFile file, uint idx, ChdTrackMetadata metadata)
    {
        var meta = GetMetadataText(file, "CHT2", idx);
        if (meta != null)
        {
            ParseTrackMetadata(meta, metadata);
            return true;
        }

        meta = GetMetadataText(file, "CHT1", idx);
        if (meta != null)
        {
            ParseTrackMetadata(meta, metadata);
            return true;
        }

        meta = GetMetadataText(file, "CHGD", idx);
        if (meta != null)
        {
            ParseTrackMetadata(meta, metadata);
            return true;
        }

        /* DVD formatted track is not yet supported by libchdr, but we can fake it. A DVD only has one track, so
         * if we're looking for the first metadata, and haven't found it yet, look for the DVD tag and go with it. */
        if (idx == 0)
        {
            meta = GetMetadataText(file, "DVD ", 0);
            if (meta != null)
            {
                metadata.Frames = 0;
                metadata.Pregap = 0;
                metadata.Postgap = 0;
                metadata.Track = 1;
                metadata.SectorOffset = 0;
                metadata.FrameOffset = 0;
                metadata.Type = "MODE1";
                metadata.Frames = (uint)(file.TotalBytes / file.UnitBytes); /* header->unitcount */
                return true;
            }
        }

        return false;
    }

    /* raw-metadata track table (the C's iteration in rc_hash_find_chd_track) —
     * exposed for the CHDSharp.Tracks vs. raw-metadata agreement test */
    /// <summary>Parses the raw-metadata track table of a CHD file.</summary>
    /// <param name="file">the CHD file</param>
    /// <returns>one entry per track: number, type, frame counts, offsets</returns>
    internal static List<(uint Track, string Type, uint Frames, uint Pregap, uint Postgap, uint SectorOffset, uint FrameOffset)> ParseTrackTable(ChdFile file)
    {
        var result = new List<(uint, string, uint, uint, uint, uint, uint)>();
        var metadata = new ChdTrackMetadata();
        uint sectorOffset = 0;
        uint frameOffset = 0;

        for (uint idx = 0; GetChdMetadata(file, idx, metadata); idx++)
        {
            metadata.SectorOffset = sectorOffset;
            sectorOffset += metadata.Frames;

            frameOffset += metadata.Pregap;
            metadata.FrameOffset = frameOffset;
            var paddingFrames = ((metadata.Frames + 3) & ~3u) - metadata.Frames;
            frameOffset += metadata.Frames + paddingFrames;

            result.Add((metadata.Track, metadata.Type, metadata.Frames, metadata.Pregap, metadata.Postgap, metadata.SectorOffset, metadata.FrameOffset));
        }

        return result;
    }

    /* rc_hash_find_chd_track */
    private static bool FindChdTrack(ChdFile file, uint track, ChdTrackMetadata metadata)
    {
        uint largestSize = 0;
        uint largestIdx = 0;
        uint sectorOffset = 0;
        uint frameOffset = 0;
        uint idx;

        /* CHD doesn't keep track of sessions. Assume the first session is a single track and hope for the best */
        if (track == ConsoleIds.RcHashCdtrackFirstOfSecondSession)
        {
            track = 2;
        }

        for (idx = 0;; idx++)
        {
            if (!GetChdMetadata(file, idx, metadata))
                break;

            /* calculate the actual sector offset of the track */
            metadata.SectorOffset = sectorOffset;
            sectorOffset += metadata.Frames;

            /* calculate the frame offset within the CHD. this logic is stolen from the
             * RetroArch implementation. I don't see anything in the CHD documentation
             * that explains the need for it. Apparently each track is padded to a
             * multiple of 4 frames, regardless of the number of frames in a hunk. */
            frameOffset += metadata.Pregap;
            metadata.FrameOffset = frameOffset;
            var paddingFrames = ((metadata.Frames + 3) & ~3u) - metadata.Frames;
            frameOffset += metadata.Frames + paddingFrames;

            if (metadata.Track == track)
                return true;

            if (string.Equals(metadata.Type, "AUDIO", StringComparison.Ordinal))
                continue;

            if (track == ConsoleIds.RcHashCdtrackFirstData)
                return true;

            if (metadata.Frames > largestSize)
            {
                largestSize = metadata.Frames;
                largestIdx = idx;
            }
        }

        switch (track)
        {
            case ConsoleIds.RcHashCdtrackLast:
                return true;

            case ConsoleIds.RcHashCdtrackLargest:
                if (idx == largestIdx)
                    return true;

                return GetChdMetadata(file, largestIdx, metadata);

            default:
                return false;
        }
    }

    /* rc_hash_handle_chd_read_sector */
    private static int ReadSector(object? trackHandle, uint sector, byte[] buffer, int requestedBytes)
    {
        var chdTrack = (ChdTrackHandle?)trackHandle;
        if (chdTrack == null)
            return 0;

        ChdFile file = chdTrack.File;
        var bytesRead = 0;

        if (sector < chdTrack.FirstSector)
            return 0;

        /* convert the real sector to the chd frame and then use that to find the hunk that contains it */
        var chdFrame = sector - chdTrack.FirstSector;
        if (chdFrame > chdTrack.FramesInTrack)
            return 0;

        chdFrame += chdTrack.FirstFrame;

        var hunk = chdFrame / chdTrack.FramesPerHunk;
        var offset = (chdFrame % chdTrack.FramesPerHunk) * file.UnitBytes + chdTrack.SectorHeaderSize;

        do
        {
            if (hunk != chdTrack.HunkNum)
            {
                if (file.ReadHunk(hunk, chdTrack.HunkMem) != ChdError.Chderrnone)
                    return bytesRead;

                chdTrack.HunkNum = hunk;
            }

            if (requestedBytes <= chdTrack.SectorDataSize)
            {
                Array.Copy(chdTrack.HunkMem, (int)offset, buffer, 0, requestedBytes);
                bytesRead += requestedBytes;
                break;
            }

            Array.Copy(chdTrack.HunkMem, (int)offset, buffer, bytesRead, (int)chdTrack.SectorDataSize);
            bytesRead += (int)chdTrack.SectorDataSize;
            requestedBytes -= (int)chdTrack.SectorDataSize;

            offset += file.UnitBytes;
            if (offset >= file.HunkBytes)
            {
                /* the C compares `>` (HashCHD.cpp:225), overreading the hunk
                 * buffer on an exact boundary; `>=` is unreachable on the
                 * parity paths (requestedBytes <= SectorDataSize everywhere)
                 * and avoids a crash here */
                offset = chdTrack.SectorHeaderSize;
                hunk++;
            }
        } while (true);

        return bytesRead;
    }

    private static bool IsNonZeroHash(byte[]? hash)
    {
        if (hash == null)
            return false;

        foreach (var value in hash)
        {
            if (value != 0)
                return true;
        }

        return false;
    }

    private static bool HashEquals(byte[]? candidate, byte[]? expected)
    {
        if (candidate == null || expected == null || candidate.Length != expected.Length)
            return false;

        return candidate.AsSpan().SequenceEqual(expected);
    }

    /* locate and open the parent of a differential (child) CHD. RALibretro
     * 1.8.4 scans the child's directory for a CHD whose header hashes match
     * the parent hashes recorded in the child (HashCHD.cpp). */
    private static ChdError OpenChdFile(string path, RcHashIterator iterator, out ChdFile? file)
    {
        file = null;

        var err = ChdFile.Open(path, out file);
        if (err == ChdError.Chderrnone)
            return err;

        if (err != ChdError.Chderrrequiresparent)
        {
            HashEngine.IteratorErrorFormatted(iterator, "chd_open failed: {0}", ChdErrorString(err));
            return err;
        }

        HashEngine.IteratorVerbose(iterator, "Parent CHD required");

        var headerErr = Chd.ReadHeader(path, out var childHeader);
        if (headerErr != ChdError.Chderrnone || childHeader == null)
        {
            HashEngine.IteratorErrorFormatted(iterator, "chd_read_header failed: {0}", ChdErrorString(headerErr));
            return headerErr;
        }

        var filename = HashEngine.PathGetFilename(path);
        var directory = path.Substring(0, path.Length - filename.Length);
        if (directory.Length == 0)
            directory = ".";
        else if (directory.EndsWith(':'))
            directory += "\\";

        List<string> matchingFiles;
        try
        {
            matchingFiles = Directory.EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .Where(name => !string.IsNullOrEmpty(name) && !name.StartsWith('.') &&
                               string.Equals(Path.GetExtension(name), ".chd", StringComparison.OrdinalIgnoreCase))
                .Select(name => name!)
                .ToList();
        }
        catch
        {
            HashEngine.IteratorErrorFormatted(iterator, "Failed to locate parent chd: {0}", "iterate directory failed");
            return err;
        }

        if (matchingFiles.Count == 0)
        {
            HashEngine.IteratorErrorFormatted(iterator, "Failed to locate parent chd: {0}", "iterate directory failed");
            return err;
        }

        /* assume the parent file will have a similar name and prioritize
         * anything that starts with the same first four characters */
        var prioritized = new List<string>(matchingFiles.Count);
        foreach (var match in matchingFiles)
        {
            if (match.Length > 4 && filename.Length > 4 &&
                string.Compare(match, 0, filename, 0, 4, StringComparison.OrdinalIgnoreCase) == 0)
            {
                prioritized.Add(match);
            }
        }

        foreach (var match in matchingFiles)
        {
            if (!prioritized.Any(item => string.Equals(item, match, StringComparison.Ordinal)))
                prioritized.Add(match);
        }

        var hasMd5 = IsNonZeroHash(childHeader.Md5);
        var hasSha1 = IsNonZeroHash(childHeader.Sha1);

        HashEngine.IteratorVerboseFormatted(iterator, "Scanning for parent chd in {0}", directory);

        string? parentPath = null;
        var parentsTried = 0;
        foreach (var match in prioritized)
        {
            if (match.Length == filename.Length && string.Equals(match, filename, StringComparison.OrdinalIgnoreCase))
                continue;

            ++parentsTried;

            var parentPathCandidate = directory.EndsWith('/') || directory.EndsWith('\\')
                ? directory + match
                : directory + "/" + match;

            if (Chd.ReadHeader(parentPathCandidate, out var parentHeader) != ChdError.Chderrnone || parentHeader == null)
                continue;

            if (hasMd5 && !HashEquals(parentHeader.Md5, childHeader.ParentMd5))
                continue;
            if (hasSha1 && !HashEquals(parentHeader.Sha1, childHeader.ParentSha1))
                continue;

            HashEngine.IteratorVerboseFormatted(iterator, "Found parent chd ({0} tried): {1}", parentsTried, match);
            parentPath = parentPathCandidate;
            break;
        }

        if (parentPath == null)
        {
            HashEngine.IteratorError(iterator, "Could not find parent chd");
            return err;
        }

        err = ChdFile.Open(path, parentPath, out file);
        if (err != ChdError.Chderrnone)
        {
            HashEngine.IteratorErrorFormatted(iterator, "chd_open for child failed: {0}", ChdErrorString(err));
        }

        return err;
    }

    /* rc_hash_handle_chd_open_track */
    private static object? OpenTrackIterator(string path, uint track, RcHashIterator iterator)
    {
        ChdError err = OpenChdFile(path, iterator, out var file);
        if (err != ChdError.Chderrnone)
        {
            return null;
        }

        var metadata = new ChdTrackMetadata();
        if (!FindChdTrack(file!, track, metadata))
        {
            file!.Dispose(); /* chd_close */
            return null;
        }

        var chdTrack = new ChdTrackHandle
        {
            File = file!,
            HunkNum = uint.MaxValue,
            HunkMem = new byte[file!.HunkBytes],
            FramesPerHunk = file.HunkBytes / file.UnitBytes,
            FirstSector = metadata.SectorOffset,
            FirstFrame = metadata.FrameOffset,
            FramesInTrack = metadata.Frames
        };

        /* https://github.com/libyal/libodraw/blob/main/documentation/Optical%20disc%20RAW%20format.asciidoc */
        if (string.Equals(metadata.Type, "MODE1_RAW", StringComparison.Ordinal))
        {
            /* 16-byte header, 2048 bytes data, 288 byte footer */
            chdTrack.SectorDataSize = 2048;
            chdTrack.SectorHeaderSize = 16;
            return chdTrack;
        }
        else if (string.Equals(metadata.Type, "MODE2_RAW", StringComparison.Ordinal))
        {
            /* MODE2: 16-byte header, 2336 bytes data */
            /* MODE2 XA1: 16-byte header, 8 byte subheader, 2048 bytes data */
            /* MODE2 XA2: 16-byte header, 8 byte subheader, 2324 bytes data */

            /* assume MODE2 until we know otherwise */
            chdTrack.SectorDataSize = 2336;
        }
        else if (string.Equals(metadata.Type, "MODE1", StringComparison.Ordinal))
        {
            /* 2048 bytes of data from MODE1_RAW without header/footer */
            chdTrack.SectorDataSize = 2048;
            chdTrack.SectorHeaderSize = 0;
            return chdTrack;
        }
        else if (string.Equals(metadata.Type, "AUDIO", StringComparison.Ordinal))
        {
            /* 2352 bytes of raw data */
            chdTrack.SectorDataSize = 2352;
            chdTrack.SectorHeaderSize = 0;
            return chdTrack;
        }
        else
        {
            /* libchdr claims all sectors are 2448 bytes (header->unitbytes).
             * assume the whole sector is used, and we'll try to determine a more appropiate size */
            chdTrack.SectorDataSize = 2352;
            chdTrack.SectorHeaderSize = 0;
        }

        /* read the first 32 bytes of sector 16 (TOC) so we can attempt to identify the disc format */
        var buffer = new byte[32];
        if (ReadSector(chdTrack, chdTrack.FirstSector + 16, buffer, buffer.Length) != buffer.Length)
        {
            CloseTrack(chdTrack);
            return null;
        }

        /* if this is a CDROM-XA data source, the "CD001" tag will be 25 bytes into the sector */
        if (Matches(buffer, 25, "CD001", 5))
        {
            /* MODE2 XA1: 16-byte header, 8 byte subheader, 2048 bytes data */
            /* MODE2 XA2: 16-byte header, 8 byte subheader, 2324 bytes data */
            /* subheader[2] & 0x20 indicates the XA form */
            chdTrack.SectorDataSize = (buffer[16 + 2] & 0x20) != 0 ? 2324u : 2048u;
            chdTrack.SectorHeaderSize = 24;
        }
        /* otherwise it should be 17 bytes into the sector */
        else if (Matches(buffer, 17, "CD001", 5))
        {
            /* MODE0: 16-byte header, 2336 bytes data */
            /* MODE1: 16-byte header, 2048 bytes data, 288 byte footer */
            /* MODE2: 16-byte header, 2336 bytes data */
            /* header[15] & 0x03 indicates the mode */
            chdTrack.SectorDataSize = (buffer[15] & 3) == 1 ? 2048u : 2336u;
            chdTrack.SectorHeaderSize = 16;
        }
        /* also check for data not containing header/footer */
        else if (Matches(buffer, 1, "CD001", 5))
        {
            /* with no header data, we can't determine the mode, assume 2048 as that's the most common format */
            chdTrack.SectorDataSize = 2048;
            chdTrack.SectorHeaderSize = 0;
        }
        /* if we didn't find a CD001 tag, this format may predate ISO-9660 */
        /* ISO-9660 says the first twelve bytes of a sector should be the sync pattern 00 FF FF FF FF FF FF FF FF FF FF 00 */
        else if (Matches(buffer, 0, SyncPattern, SyncPattern.Length))
        {
            /* after the 12 byte sync pattern is three bytes identifying the sector and then one byte for the mode (total 16 bytes) */
            /* MODE0 and MODE2 are both 2336 bytes of data. MODE1 is 2048 bytes */
            chdTrack.SectorDataSize = (buffer[15] & 3) == 1 ? 2048u : 2336u;
            chdTrack.SectorHeaderSize = 16;
        }
        else
        {
            /* with no header data, we can't determine the mode, assume 2048 as that's the most common format */
            chdTrack.SectorDataSize = 2048;
            chdTrack.SectorHeaderSize = 0;
        }

        return chdTrack;
    }

    /* rc_hash_handle_chd_first_track_sector */
    private static uint FirstTrackSector(object? trackHandle)
    {
        var chdTrack = (ChdTrackHandle?)trackHandle;
        return chdTrack?.FirstSector ?? 0;
    }

    /* rc_hash_handle_chd_close_track */
    private static void CloseTrack(object? trackHandle)
    {
        var chdTrack = (ChdTrackHandle?)trackHandle;
        if (chdTrack != null)
        {
            chdTrack.File.Dispose(); /* chd_close */
        }
    }

    /* rc_hash_init_chd_cdreader */
    /// <summary>rc_hash_init_chd_cdreader</summary>
    public static void InitChdCdreader()
    {
        var cdreader = new RcHashCdreader
        {
            OpenTrackIterator = OpenTrackIterator,
            ReadSector = ReadSector,
            CloseTrack = CloseTrack,
            FirstTrackSector = FirstTrackSector
        };

        HashEngine.InitCustomCdreader(cdreader);
    }
}

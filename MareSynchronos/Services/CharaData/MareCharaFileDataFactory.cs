using ShoninSync.API.Data;
using ShoninSync.FileCache;
using ShoninSync.Services.CharaData.Models;

namespace ShoninSync.Services.CharaData;

public sealed class MareCharaFileDataFactory
{
    private readonly FileCacheManager _fileCacheManager;

    public MareCharaFileDataFactory(FileCacheManager fileCacheManager)
    {
        _fileCacheManager = fileCacheManager;
    }

    public MareCharaFileData Create(string description, CharacterData characterCacheDto)
    {
        return new MareCharaFileData(_fileCacheManager, description, characterCacheDto);
    }
}
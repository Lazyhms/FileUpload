using FileUpload.Entity;
using FileUpload.Filters;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using System.IO.Hashing;
using System.Security.Cryptography;
using System.Text;

namespace FileUpload.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FileController : ControllerBase
{
    private readonly FileDbContext _dbContext;
    private readonly string _baseDirectory = Path.Combine(AppContext.BaseDirectory, "Software");

    public FileController(FileDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 检查相同MD5码文件是否存在
    /// </summary>
    /// <returns></returns>
    [HttpGet("check/{md5?}")]
    public async Task<IActionResult> Check(string? md5)
    {
        IQueryable<FileDo> query = _dbContext.File;
        if (!string.IsNullOrWhiteSpace(md5))
        {
            query = query.Where(w => w.MD5 == md5);
        }
        var result = await query.ToListAsync();
        if (result.Any())
        {
            return Ok(result);
        }
        else
        {
            return NoContent();
        }
    }

    /// <summary>
    /// 上传文件
    /// </summary>
    /// <returns></returns>
    [HttpPost("Upload/{identity}")]
    [DisableRequestSizeLimit]
    [DisableFormValueModelBinding]
    [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue)]
    public async Task<IActionResult> UploadAsync(string identity, int index)
    {
        if (!Request.HasFormContentType)
        {
            return BadRequest();
        }

        var boundary = Request.GetMultipartBoundary();
        if (string.IsNullOrWhiteSpace(boundary))
        {
            return BadRequest();
        }

        var reader = new MultipartReader(boundary, Request.Body);
        var section = await reader.ReadNextSectionAsync();
        while (section != null)
        {
            var header = section.GetContentDispositionHeader();
            if (header!.IsFileDisposition())
            {
                var tempPath = Path.Combine(_baseDirectory, "temp", $"{identity}");
                if (!Directory.Exists(tempPath))
                {
                    Directory.CreateDirectory(tempPath);
                }

                var fileSection = section.AsFileSection();
                var fullFilePath = Path.Combine(tempPath, fileSection!.FileName);
                using var file = new FileStream(fullFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite, 1024 * 1024, useAsync: true);
                fileSection!.FileStream!.Seek(0, SeekOrigin.Begin);
                await fileSection!.FileStream!.CopyToAsync(file);
                await file.DisposeAsync();
            }
            section = await reader.ReadNextSectionAsync();
        }

        return NoContent();
    }

    /// <summary>
    /// 合并文件
    /// </summary>
    /// <returns></returns>
    [HttpPost("Merger/{identity}")]
    public async Task<IActionResult> MergerAsync(string identity, Dictionary<string, string> parameter)
    {
        var tempPath = Path.Combine(_baseDirectory, "temp", $"{identity}");
        var tempFiles = Directory.GetFiles(tempPath);

        var fileName = $"{Path.GetFileNameWithoutExtension(parameter["FileName"])}_{Guid.NewGuid():N}{Path.GetExtension(parameter["FileName"])}";
        var fullFilePath = Path.Combine(_baseDirectory, fileName);
        using var fileStream = new FileStream(fullFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite, 1024 * 1024, useAsync: true);

        foreach (var item in tempFiles)
        {
            using var tempStream = new FileStream(item, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite, 1024 * 1024, useAsync: true);
            await tempStream.CopyToAsync(fileStream);
            await tempStream.DisposeAsync();
        }

        var fileByte = new byte[fileStream.Length];
        fileStream.Seek(0, SeekOrigin.Begin);
        await fileStream.ReadAsync(fileByte);
        await fileStream.DisposeAsync();

        Directory.Delete(tempPath, true);

        var file = new FileDo
        {
            MD5 = parameter["MD5"],
            FileName = fileName,
            Crc32 = Convert.ToHexString(Crc32.Hash(fileByte).Reverse().ToArray())
        };
        await _dbContext.File.AddAsync(file);
        await _dbContext.SaveChangesAsync();

        return Ok(file);
    }


    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    [HttpGet("download")]
    public async Task<FileResult> DownLoadAsnyc(string file)
    {
        var filePath = Path.Combine(_baseDirectory, file);

        var fileStream = new FileStream(filePath, FileMode.Open);

        var result = new FileStreamResult(fileStream, "application/octet-stream")
        {
            EntityTag = new EntityTagHeaderValue("\"TestFile\"", true),
            EnableRangeProcessing = true
        };

        return result;
    }
}
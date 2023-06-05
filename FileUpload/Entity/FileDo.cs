using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FileUpload.Entity;

[Table("File")]
public class FileDo
{
    /// <summary>
    /// 
    /// </summary>
    [Key]
    public string MD5 { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public string FileName { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public string Crc32 { get; set; }
}
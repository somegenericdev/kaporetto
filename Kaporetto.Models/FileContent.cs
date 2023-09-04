using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.VisualBasic.CompilerServices;

namespace Kaporetto.Models;
[Table("FileContent")]
public class FileContent
{
    [Key]
    public string sha256 { get; set; }
    public byte[] content { get; set; }
    public byte[] thumb { get; set; }

    public FileContent()
    {
        
    }

    public FileContent(byte[] _content, byte[] _thumb, string _sha256)
    {
        content = _content;
        thumb = _thumb;
        sha256 = _sha256;
    }
}
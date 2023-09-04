using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Kaporetto.Models;
[Table("File")]
public class File
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public long id { get; set; }
    public int size { get; set; }
    public int? width { get; set; }
    public int? height { get; set; }
    public string mime { get; set; }
    public string thumb { get; set; }
    public string path { get; set; }
    public long postId { get; set; }
    public string board { get; set; }
    [ForeignKey("postId,board")]
    public Post Post { get; set; }
    [ForeignKey("sha256")]
    public FileContent FileContent { get; set; }
    public string sha256 { get; set; }
    public File()
    {
        
    }
    public File(int _size,
        int? _width,
        int? _height,
        string _mime,
        string _thumb,
        string _path,
        FileContent _fileContent)
    {
        size = _size;
        width = _width;
        height = _height;
        mime = _mime;
        thumb = _thumb;
        path = _path;
        sha256 = _fileContent.sha256;
        FileContent = _fileContent;
    }
}
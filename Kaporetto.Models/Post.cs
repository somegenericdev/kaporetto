using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Kaporetto.Models;
[Table("Post")]
[PrimaryKey(nameof(postId), nameof(board))]
public class Post
{

    public string? name { get; set; }
    public string? signedRole { get; set; }
    public string? email { get; set; }
    public string? flag { get; set; }
    public string? flagName { get; set; }
    public string? id { get; set; }
    public string? subject { get; set; }
    public string? flagCode { get; set; }
    public string? markdown { get; set; }
    public string message { get; set; }
    [Column(Order = 0)]
    public long postId { get; set; }
    public long? parentId { get; set; }
    public DateTime creation { get; set; }
    [Column(Order = 1)]

    public string board { get; set; }
    public virtual ImmutableList<File> files { get; set; }
    public bool isThread { get; set; }


    public Post()
    {
    }

    public Post(string _name, string _signedRole, string _email, string _flag, string _flagName, string _id,
        string _subject, string _flagCode, string _markdown, string _message, long _postId, DateTime _creation,
        ImmutableList<File> _files, bool _isThread, long? _parentId, string _board) //reinstanciate
    {
        name = _name;
        signedRole = _signedRole;
        email = _email;
        flag = _flag;
        flagName = _flagName;
        id = _id;
        subject = _subject;
        flagCode = _flagCode;
        markdown = _markdown;
        message = _message;
        postId = _postId;
        creation = _creation;
        files = _files;
        isThread = _isThread;
        parentId = _parentId;
        board = _board;
    }

    public Post(PostContainer postContainer)
    {
        postId = postContainer.threadId;
        name = postContainer.name;
        signedRole = postContainer.signedRole;
        email = postContainer.email;
        flag = postContainer.flag;
        flagName = postContainer.flagName;
        id = postContainer.id;
        subject = postContainer.subject;
        flagCode = postContainer.flagCode;
        markdown = postContainer.markdown;
        message = postContainer.message;
        creation = postContainer.creation;
        files = postContainer.files;
        isThread = true;
        parentId = null;
    }
}
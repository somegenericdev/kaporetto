using System.Text;
using Kaporetto.DbConnector.Models;
using Kaporetto.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;

namespace Kaporetto.DbConnector;

public class Worker : BackgroundService
{
    private readonly KaporettoContext DbContext;
    private IConnection _connection;
    private IModel _channel;

    public Worker(KaporettoContext dbContext)
    {
        DbContext = dbContext;
        InitRabbitMQ();
    }


    private void InitRabbitMQ()
    {
        var factory = new ConnectionFactory { HostName = "localhost" };

        // create connection  
        _connection = factory.CreateConnection();

        // create channel  
        _channel = _connection.CreateModel();

        // _channel.ExchangeDeclare("demo.exchange", ExchangeType.Topic);
        // _channel.QueueDeclare("demo.queue.log", false, false, false, null);
        // _channel.QueueBind("demo.queue.log", "demo.exchange", "demo.queue.*", null);
        _channel.BasicQos(0, 1, false);

        // _connection.ConnectionShutdown += RabbitMQ_ConnectionShutdown;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += (ch, ea) =>
        {
            try
            {
              
                    // received message  
                    var content = Encoding.UTF8.GetString(ea.Body.ToArray());
                    var post = JsonConvert.DeserializeObject<Post>(content);
                    
                    post.files.ForEach(file =>
                    {
                        var fileContent=DbContext.FileContents.FirstOrDefault(f => f.sha256 == file.FileContent.sha256);
                        if (fileContent != null)
                        {
                            file.FileContent = fileContent;
                        }
                    });
                    DbContext.Posts.Add(post);
                    DbContext.SaveChanges();
                    // handle the received message  
                    _channel.BasicAck(ea.DeliveryTag, false);
                    Log.Logger.Information($"[{DateTime.Now.ToString()}] Saved and acked post no. {post.postId}");
                    DbContext.ChangeTracker.Clear();
            }
            catch(Exception e)
            {
                Log.Logger.Error(e.ToString());
                _channel.BasicNack(ea.DeliveryTag, false,false);
                DbContext.ChangeTracker.Clear();

            }
        };


        _channel.BasicConsume("Posts", false, consumer);
        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _channel.Close();
        _connection.Close();
        base.Dispose();
    }
}
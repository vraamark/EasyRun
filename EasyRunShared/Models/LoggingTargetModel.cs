using System.Collections.ObjectModel;

namespace EasyRun.Models
{
    public class LoggingTargetModel
    {
        public string Text { get; set; }
        public string YamlName { get; set; }
        public LoggingTargetType TargetType { get; set; }
        public bool AsExtension { get; set; }

        public static ObservableCollection<LoggingTargetModel> GetDefault()
        {
            return new ObservableCollection<LoggingTargetModel>
            {
                new LoggingTargetModel {Text = "Console", YamlName = "", TargetType = LoggingTargetType.Console, AsExtension = false },
                new LoggingTargetModel {Text = "Seq", YamlName = "seq", TargetType = LoggingTargetType.Seq, AsExtension = true },
                new LoggingTargetModel {Text = "Seq Url", YamlName = "seq", TargetType = LoggingTargetType.SeqUrl, AsExtension = false },
                new LoggingTargetModel {Text = "Elastic Stack", YamlName = "elastic", TargetType = LoggingTargetType.Elastic, AsExtension = true },
                new LoggingTargetModel {Text = "Elastic Stack Url", YamlName = "elastic", TargetType = LoggingTargetType.ElasticUrl, AsExtension = false },
            };
        }
    }
}

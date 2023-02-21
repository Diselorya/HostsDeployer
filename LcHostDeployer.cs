using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.IO;

namespace HostsDeployer
{
    public class LcHostDeployer
    {
        public static string AddSignal { get; } = "+";
        public static string RemoveSignal { get; } = "-";
        public static string CommentSignal { get; } = "#";
        public static string ConfigAddress { get; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HostsDeploymentConfig.txt");
        public static string Pattern { get; } = @"^(\s*[#+-]\s*){0,1}(\S+)\s+(\S+)\s*(#\s+(.*)){0,1}$";

        public Dictionary<string, LcHost> ToComment { get; protected set; } = new Dictionary<string, LcHost>();
        public Dictionary<string, LcHost> ToAdd { get; protected set; } = new Dictionary<string, LcHost>();
        public Dictionary<string, LcHost> ToRemove { get; protected set; } = new Dictionary<string, LcHost>();

        public string[] Lines { get; protected set; } = null;

        public virtual bool IsEmpty => this.ToComment.Count + this.ToAdd.Count + this.ToRemove.Count < 1;

        public LcHostDeployer() => this.Load();
        public LcHostDeployer(string configAddress) => this.Load(configAddress);

        public LcHostDeployer Load(string configAddress = null)
        {
            if (configAddress is null) configAddress = LcHostDeployer.ConfigAddress;
            this.Reset();
            if (!File.Exists(configAddress)) return null;

            this.Lines = File.ReadAllLines(configAddress);
            foreach(string line in this.Lines)
            {
                Match m = Regex.Match(line, LcHostDeployer.Pattern);
                if(m.Success)
                {
                    string op = m.Groups[1].ToString();
                    LcHost host = new LcHost(line.Substring(op.Length));
                    if (op.Trim() == "+")
                        this.ToAdd[host.Name] = host;
                    else if (op.Trim() == "-")
                        this.ToRemove[host.Name] = host;
                    else if (op.Trim() == "#")
                    {
                        host.Enable = false;
                        this.ToComment[host.Name] = host;
                    }
                }
            }

            return this;
        }

        public LcHostDeployer Reset()
        {
            this.Lines = null;
            this.ToComment.Clear();
            this.ToAdd.Clear();
            this.ToRemove.Clear();
            return this;
        }

        public bool Apply()
        {
            if(this.IsEmpty) return false;

            LcHosts hosts = new LcHosts();
            if(this.ToAdd.Count > 0)
            {
                foreach(var host in this.ToAdd)
                    hosts.SetHost(host.Value);
            }
            if (this.ToRemove.Count > 0)
            {
                foreach (var host in this.ToRemove)
                    hosts.RemoveHost(host.Value);
            }
            if (this.ToComment.Count > 0)
            {
                foreach (var host in this.ToComment)
                    hosts.ModifyHost(host.Value);
            }

            return !string.IsNullOrEmpty(hosts.Apply());
        }

        public override string ToString()
        {
            if (this.IsEmpty) return "";
            StringBuilder sb = new StringBuilder();
            if (this.ToAdd.Count > 0)
            {
                foreach (var host in this.ToAdd)
                    sb.Append("新增：").Append(host.Value.ToString()).AppendLine();
            }
            if (this.ToRemove.Count > 0)
            {
                foreach (var host in this.ToRemove)
                    sb.Append("删除：").Append(host.Value.ToString()).AppendLine();
            }
            if (this.ToComment.Count > 0)
            {
                foreach (var host in this.ToComment)
                    sb.Append("注释：").Append(host.Value.ToString()).AppendLine();
            }
            return sb.ToString();
        }
    }
}

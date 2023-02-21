using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.IO;
using ExcelDataReader;
using System.Data;

namespace HostsDeployer
{
    public class LcHostDeployer
    {
        public static string AddSignal { get; } = "+";
        public static string RemoveSignal { get; } = "-";
        public static string CommentSignal { get; } = "#";
        public virtual string ConfigAddress { get; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HostsDeploymentConfig.txt");
        public static string Pattern { get; } = @"^(\s*[#+-]\s*){0,1}(\S+)\s+(\S+)\s*(#\s+(.*)){0,1}$";

        public Dictionary<string, LcHost> ToComment { get; protected set; } = new Dictionary<string, LcHost>();
        public Dictionary<string, LcHost> ToAdd { get; protected set; } = new Dictionary<string, LcHost>();
        public Dictionary<string, LcHost> ToRemove { get; protected set; } = new Dictionary<string, LcHost>();

        public LcHosts Hosts { get; protected set; } = new LcHosts();

        public string[] Lines { get; protected set; } = null;

        public virtual bool IsEmpty => this.ToComment.Count + this.ToAdd.Count + this.ToRemove.Count < 1;

        public LcHostDeployer() => this.Load();
        public LcHostDeployer(string configAddress) => this.Load(configAddress);

        public virtual LcHostDeployer Load(string configAddress = null)
        {
            if (configAddress is null) configAddress = this.ConfigAddress;
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
                        this.ToAdd[host.Id] = host;
                    else if (op.Trim() == "-")
                        this.ToRemove[host.Id] = host;
                    else if (op.Trim() == "#")
                    {
                        host.Enable = false;
                        this.ToComment[host.Id] = host;
                    }
                }
            }

            return this;
        }

        public virtual LcHostDeployer Reset()
        {
            this.Lines = null;
            this.ToComment.Clear();
            this.ToAdd.Clear();
            this.ToRemove.Clear();
            if (this.Hosts is null) this.Hosts = new LcHosts();
            else this.Hosts.Reset();
            return this;
        }

        public virtual bool Apply()
        {
            if(this.IsEmpty) return false;

            if(this.ToAdd.Count > 0)
            {
                foreach(var host in this.ToAdd)
                {
                    this.Hosts.SetHost(host.Value);
                }
            }
            if (this.ToRemove.Count > 0)
            {
                foreach (var host in this.ToRemove)
                    this.Hosts.RemoveHost(host.Value);
            }
            if (this.ToComment.Count > 0)
            {
                foreach (var host in this.ToComment)
                    this.Hosts.ModifyHost(host.Value);
            }

            return !string.IsNullOrEmpty(this.Hosts.Apply());
        }

        public override string ToString()
        {
            if (this.IsEmpty) return "";
            StringBuilder sb = new StringBuilder();
            sb.Append($"根据 {this.ConfigAddress} 修改 hosts：").AppendLine();

            //if (this.ToAdd.Count > 0)
            //{
            //    foreach (var host in this.ToAdd)
            //        sb.Append("新增：").Append(host.Value.ToString()).AppendLine();
            //}
            //if (this.ToRemove.Count > 0)
            //{
            //    foreach (var host in this.ToRemove)
            //        sb.Append("删除：").Append(host.Value.ToString()).AppendLine();
            //}
            //if (this.ToComment.Count > 0)
            //{
            //    foreach (var host in this.ToComment)
            //        sb.Append("注释：").Append(host.Value.ToString()).AppendLine();
            //}

            sb.Append(this.Hosts.Log);

            sb.Append($"Hosts 更新完成，计划新增 {this.ToAdd.Count} 项，删除 {this.ToRemove.Count} 项，注释 {this.ToComment.Count} 项；" +
                $"实际新增 {this.Hosts.Added} 项，删除 {this.Hosts.Removed} 项，修改 {this.Hosts.Modified} 项。").AppendLine();
            return sb.ToString();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace HostsDeployer
{
    public class LcHosts
    {
        #region 属性
        public static string SystemPath { get; set; } = Path.Combine(Environment.SystemDirectory, "drivers", "etc");
        public static string SystemAddress { get; set; } = Path.Combine(LcHosts.SystemPath, "hosts");
        public static string CommentSignal { get; } = "#";
        public static string TempAddress { get; set; } = Path.Combine(Path.GetTempPath(), "hosts");
        public static string ThisDirectory { get; set; } = AppDomain.CurrentDomain.BaseDirectory;
        public static string BackupDirectory { get; set; } = Path.Combine(LcHosts.ThisDirectory, "Backup");

        public string Directory { get; set; } = LcHosts.SystemPath;
        public string Address { get; set; } = LcHosts.SystemAddress;
        #endregion

        #region 数据
        public List<string> Lines { get; protected set; } = new List<string>();
        public string Text => this.Lines is null ? "" : string.Join("\n", this.Lines).Replace("\n\n\n", "\n\n");

        protected Dictionary<int, LcHost> m_OriHostsPos = new Dictionary<int, LcHost>();
        public Dictionary<string, List<string>> HostsNames = new Dictionary<string, List<string>>();
        #endregion

        #region 加载和构建
        public LcHosts()
        {
            this.Load();
        }

        public LcHosts(string hostAddress)
        {
            this.Load(hostAddress);
        }

        public virtual LcHosts Load(string hostAddress = "")
        {
            if (string.IsNullOrEmpty(hostAddress)) hostAddress = this.Address;
            if (string.IsNullOrEmpty(hostAddress)) hostAddress = LcHosts.SystemAddress;

            if(!File.Exists(hostAddress)) return null;

            this.Lines.AddRange(File.ReadAllLines(hostAddress));
            this.Load(this.Lines);

            // 备份
            if(!System.IO.Directory.Exists(LcHosts.BackupDirectory))
                System.IO.Directory.CreateDirectory(LcHosts.BackupDirectory);
            string backFilePath = Path.Combine(LcHosts.BackupDirectory, "host_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            //if (!File.Exists(backFilePath)) File.Create(backFilePath);
            File.WriteAllLines(backFilePath, this.Lines);

            return this;
        }

        protected virtual LcHosts Load(List<string> lines)
        {
            if (lines is null) lines = this.Lines;
            this.m_OriHostsPos.Clear();
            this.HostsNames.Clear();

            if(lines is null || lines.Count == 0) return null;
            for (int i = 0; i < lines.Count; i++)
                this.LoadHost(i, lines[i]);
            return this;
        }
        #endregion

        public virtual int GetHostLineNumber(string name, string address)
        {
            int[] keys = m_OriHostsPos.Keys.ToArray();
            for(int i = 0; i < m_OriHostsPos.Count; i++)
            {
                LcHost host = m_OriHostsPos[keys[i]];
                if(name.ToLower() == host.Name.ToLower() && address.ToLower() == host.Address.ToLower())
                    return keys[i];
            }
            return -1;
        }
        public virtual List<int> GetHostLineNumbers(Predicate<LcHost> predicate)
        {
            List<int> poses = new List<int>();
            int[] keys = m_OriHostsPos.Keys.ToArray();
            for (int i = 0; i < m_OriHostsPos.Count; i++)
            {
                LcHost host = m_OriHostsPos[keys[i]];
                if (predicate(host)) poses.Add(keys[i]);
            }
            return poses;
        }
        public virtual List<int> GetHostLineNumbersByName(string name)
            => GetHostLineNumbers(h => h.Name.ToLower() == name.ToLower());
        public virtual List<int> GetHostLineNumbersByAddress(string address)
            => GetHostLineNumbers(h => h.Address.ToLower() == address.ToLower());

        // 从 Lines 中读取一个 Host
        protected bool LoadHost(int lineNumber, string text) => this.LoadHost(lineNumber, new LcHost(text));
        protected bool LoadHost(int lineNumber, string name, string address, bool enable = true, string comments = null)
            => this.LoadHost(lineNumber, new LcHost(name, address, enable, comments));
        protected virtual bool LoadHost(int lineNumber, LcHost host)
        {
            if (host is null || host.IsError) return false;

            m_OriHostsPos[lineNumber] = host;
            if (!this.HostsNames.ContainsKey(host.Name) || this.HostsNames[host.Name] is null)
                this.HostsNames[host.Name] = new List<string>();
            this.HostsNames[host.Name].Add(host.Address);

            return true;
        }

        // 添加一个 Host 到 Hosts 中
        protected bool AddHost(int lineNumber, string text) => this.AddHost(lineNumber, new LcHost(text));
        protected bool AddHost(int lineNumber, string name, string address, bool enable = true, string comments = null)
            => this.AddHost(lineNumber, new LcHost(name, address, enable, comments));
        protected virtual bool AddHost(int lineNumber, LcHost host)
        {
            if (host is null || host.IsError) return false;

            // 只允许在文件末尾追加
            if (lineNumber < this.Lines.Count) lineNumber = this.Lines.Count + 1;
            this.Lines.Add(host.ToString());
            return this.LoadHost(lineNumber, host);
        }

        // 设置一个 Host（自动判断修改还是添加）
        public int SetHost(string text) => this.SetHost(new LcHost(text));
        public int SetHost(string name, string address, bool enable = true, string comments = null)
            => this.SetHost(new LcHost(name, address, enable, comments));
        public virtual int SetHost(LcHost host)
        {
            if(host is null || host.IsError) return -1;

            int pos = this.GetHostLineNumber(host.Name, host.Address);
            if (pos < 0)
            {
                this.AddHost(this.Lines.Count, host);
                return this.Lines.Count;
            }
            else
            {
                this.Lines[pos] = host.ToString();
                this.LoadHost(pos, host);
                return pos;
            }
        }

        // 修改一个 Host
        public int ModifyHost(string text) => this.ModifyHost(new LcHost(text));
        public int ModifyHost(bool enable, string name = null, string address = null, string comments = null)
            => this.ModifyHost(new LcHost(name, address, enable, comments));
        public virtual int ModifyHost(LcHost host)
        {
            if (host is null || host.IsError) return -1;

            int pos = this.GetHostLineNumber(host.Name, host.Address);
            if (pos < 0)
            {
                return pos;
            }
            else
            {
                LcHost oriHost = new LcHost(this.Lines[pos]);
                if (string.IsNullOrEmpty(host.Name)) host.Name = oriHost.Name;
                if (string.IsNullOrEmpty(host.Address)) host.Address = oriHost.Address;
                if (string.IsNullOrEmpty(host.Comments)) host.Comments = oriHost.Comments;
                this.Lines[pos] = host.ToString();
                this.LoadHost(pos, host);
                return pos;
            }
        }

        // 删除一个 Host
        public int RemoveHost(string name, string address) => this.RemoveHost(new LcHost(name, address));
        public virtual int RemoveHost(LcHost host)
        {
            if (host is null || host.IsError) return -1;

            int pos = this.GetHostLineNumber(host.Name, host.Address);

            if(pos < 0) return -1;
            else
            {
                this.Lines[pos] = "";
                this.Load(this.Lines);
                return pos;
            }

        }
        public int RemoveHost(int pos)
            => (pos < 0 || pos >= this.Lines.Count) ? -1 : this.RemoveHost(new LcHost(this.Lines[pos - 1]));

        public virtual string Apply(string fileAddress = null)
        {
            if (string.IsNullOrEmpty(fileAddress)) fileAddress = LcHosts.SystemAddress;
            File.WriteAllText(this.Address, this.Text);
            return fileAddress;
        }
        
        public override string ToString()
        {
            if (m_OriHostsPos.Count <= 0) return "";
            StringBuilder sb = new StringBuilder();
            foreach(var pos in m_OriHostsPos.Keys)
            {
                sb.Append(m_OriHostsPos[pos]).AppendLine();
            }
            return sb.ToString();
        }
    }
}

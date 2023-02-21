using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace HostsDeployer
{
    public class LcHost
    {
        public string Address { get; set; } = "";
        public string Name { get; set; } = "";
        public bool Enable { get; set; } = true;
        public string Comments { get; set; } = null;
        public bool IsError { get; set; } = false;

        public string Id => this.Name + ":" + this.Address;

        public static string Pattern { get; } = @"^(\s*#\s*){0,1}(\S+)\s+(\S+)\s*(#\s+(.*)){0,1}$";

        public LcHost(string name, string address, bool enable = true, string comments = null)
            => this.Set(name, address, enable, comments);
        public LcHost(string text) => this.Set(text);


        public virtual LcHost Set(string name, string address, bool enable = true, string comments = null)
        {
            this.Name = name.Trim();
            this.Address = address.Trim();
            this.Enable = enable;
            this.Comments = comments;

            if (string.IsNullOrEmpty(this.Name)
                || string.IsNullOrEmpty(this.Address)
                || this.Address.StartsWith("#"))
            {
                this.IsError = true;
                return null;
            }
            return this;
        }

        public virtual LcHost Set(string text)
        {
            text = text.Trim();
            Match m = Regex.Match(text, LcHost.Pattern);
            if (!m.Success) { this.IsError = true; return null; }

            return this.Set(m.Groups[3].ToString(), m.Groups[2].ToString(), !text.StartsWith("#"), m.Groups[4].ToString());
        }

        public override string ToString()
        {
            return (this.Enable ? "" : "#\t") + this.Address + "\t" + this.Name
                + (string.IsNullOrEmpty(this.Comments) ? "" : "\t" + (this.Comments.StartsWith("#") ? "" : "# ") + this.Comments);
        }
    }
}

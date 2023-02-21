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
    public class LcHostDeployerFromExcel : LcHostDeployer
    {
        public override string ConfigAddress { get; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Hosts 部署配置.xlsx");

        public static string TableNameAdd => "Add";
        public static string TableNameRemove => "Remove";
        public static string TableNameComment => "Comment";

        public LcHostDeployerFromExcel() => this.Load();
        public LcHostDeployerFromExcel(string configAddress) => this.Load(configAddress);

        public override LcHostDeployer Load(string configAddress = null)
        {
            if (configAddress is null) configAddress = this.ConfigAddress;
            this.Reset();
            if (!File.Exists(configAddress)) return null;

            try
            {
                using (var stream = File.Open(configAddress, FileMode.Open, FileAccess.Read))
                {
                    using (IExcelDataReader reader = ExcelReaderFactory.CreateReader(stream))
                    {
                        DataSet data = reader.AsDataSet();

                        if (data.Tables.Contains(TableNameAdd))
                        {
                            DataTable table = data.Tables[TableNameAdd];
                            LoadFromDataTable(this.ToAdd, table, true);
                        }
                        if (data.Tables.Contains(TableNameRemove))
                        {
                            DataTable table = data.Tables[TableNameRemove];
                            LoadFromDataTable(this.ToRemove, table, true);
                        }
                        if (data.Tables.Contains(TableNameComment))
                        {
                            DataTable table = data.Tables[TableNameComment];
                            LoadFromDataTable(this.ToComment, table, false);
                        }
                    }
                }                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return this;
        }

        public virtual Dictionary<string, LcHost> LoadFromDataTable(Dictionary<string, LcHost> data, DataTable table, bool enable = true, int firstDataRow = 2)
        {
            if (table == null || table.Rows.Count < 1) return null;
            if (data == null) data = new Dictionary<string, LcHost>();
            if (firstDataRow < 1) firstDataRow = 1;

            for (int i = 0; i < table.Rows.Count; i++)
            {
                if(i < firstDataRow - 1) continue;
                string address = table.Rows[i][0].ToString().Trim();
                string name = table.Rows[i][1].ToString().Trim();
                string comment = table.Rows[i][2].ToString().Trim();
                LcHost host = new LcHost(name, address, enable, comment);
                if (address.Length > 0) data[host.Id] = host;
            }

            return data;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace DXample
{
    internal class Importer<T>
    {
        public static List<T> Import(){
            var dlg = new OpenFileDialog {Filter = "JSON File (*.json)|*.json"};
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<List<T>>(File.ReadAllText(dlg.FileName));
                return result;
            }
            return null;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
namespace DXample
{
    class BaiHat{
        [JsonProperty(PropertyName = "id")]
        public int ID { get; set; }
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
    }

    class YeuThich
    {
        [JsonProperty(PropertyName = "user_id")]
        public int UserID { get; set; }
        [JsonProperty(PropertyName = "song_id")]
        public int SongID { get; set; }
    }

    class KetQua
    {
        public string BaiHat { get; set; }
        public float DiemSo { get; set; }
    }

    class Dataset
    {
        public List<BaiHat> ListBaiHat { get; set; }
        public List<YeuThich> ListYeuThich { get; set; }
    }
}

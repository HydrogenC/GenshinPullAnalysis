using System.Text.Json.Nodes;
using System.Web;

namespace GenshinPullAnalysis;

internal class Program
{
    class Pull
    {
        public string Name { get; set; }
        public int Pities { get; set; }
        public bool IsSpecial { get; set; }
        public bool WonFifty { get; set; }
    }

    static readonly string[] nonSpecial = new string[]
    {
        "刻晴", "迪卢克", "七七", "莫娜", "琴", "提纳里"
    };

    const char progressBarChar = '\u2593';

    static void Main(string[] args)
    {

        var gachaUrl = new Uri(Console.ReadLine());
        var fiveStarList = new List<Pull>();
        using HttpClient client = new HttpClient();
        string responseBody = "", endId = "0";
        int page = 1;

        fiveStarList.Add(new Pull
        {
            Name = "未出",
            Pities = 0,
            IsSpecial = false,
            WonFifty = false
        });

        while (true)
        {
            Console.WriteLine($"Reading page {page}");

            var queryParts = HttpUtility.ParseQueryString(gachaUrl.Query);
            queryParts["page"] = page.ToString();
            queryParts["gacha_type"] = "301";
            queryParts["end_id"] = endId;

            string newUri = gachaUrl.AbsoluteUri.Split('?').First() + "?" + queryParts.ToString();

            using (var request = new HttpRequestMessage(HttpMethod.Get, newUri))
            {
                var response = client.Send(request);
                responseBody = response.Content.ReadAsStringAsync().Result;
                // Console.WriteLine(responseBody);
            }

            JsonNode root = JsonNode.Parse(responseBody);
            JsonArray listNode = (JsonArray)root["data"]["list"];

            if (listNode.Count == 0)
            {
                break;
            }

            foreach (var i in listNode)
            {
                string name = (string)i["name"];
                string rank = (string)i["rank_type"];

                switch (rank)
                {
                    case "5":
                        bool isSpecial = !nonSpecial.Contains(name);
                        fiveStarList[^1].WonFifty = isSpecial;

                        fiveStarList.Add(new Pull
                        {
                            Name = name,
                            Pities = 0,
                            IsSpecial = isSpecial,
                            WonFifty = false
                        });
                        break;
                    default:
                        break;
                }

                fiveStarList[^1].Pities++;
            }

            endId = (string)listNode[^1]["id"];
            page++;
            Thread.Sleep(1000);
        }

        foreach (var i in fiveStarList)
        {
            string progressBar = new string(progressBarChar, i.Pities / 2);
            string description = i.IsSpecial ? (i.WonFifty ? "小保" : "大保") : "歪";
            ConsoleColor specialColor = i.Pities >= 70 ? ConsoleColor.Red : ConsoleColor.Green;

            Console.Write("|{0} {1}\n|", i.Name, description);
            Console.ForegroundColor = specialColor;
            Console.Write("{0,-45}", progressBar);
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("| ");
            Console.ForegroundColor = specialColor;
            Console.Write("{0,-2}", i.Pities);
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("抽");
        }
    }
}
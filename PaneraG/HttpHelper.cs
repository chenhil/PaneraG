using DeathByCaptcha;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PaneraG
{
    class HttpHelper
    {
        private static Client client = null;
        private static string url = "https://www.paneracards.com/checkbalance.aspx";
        public HttpHelper()
        {
            client = (Client)new SocketClient("", "");
        }

        public async Task<string> GetRequest(string name, string gcNum)
        {
            using (System.Net.Http.HttpClient client = new System.Net.Http.HttpClient())
            {
                using (HttpResponseMessage response = await client.GetAsync(url))
                {
                    using (HttpContent content = response.Content)
                    {
                        string mycontent = await content.ReadAsStringAsync();
                        return mycontent;
                    }
                }
            }
        }

        private string GetElementById(string pattern, string content)
        {
            //tring.Format("{0}.+?value=\"(.+?)\"", ID)
            string token = Regex.Match(content, pattern).Groups[1].Value;
            return token;
        }

        //Call the Decode function by spawning a new thread
        private async Task<string> UploadCaptcha(string filename)
        {
            string captcha = null;
            await Task.Run(() => { captcha = Decode(@"Captcha\" + filename); });
            return captcha;
        }

        //Upload Captcha to deathbycaptcha server
        private string Decode(object o)
        {
            string captchaFileName = (string)o;

            //UpdateRichTextBox(string.Format("Solving {0}", captchaFileName));
            Captcha captcha = client.Upload(captchaFileName);

            if (null != captcha)
            {
                //UpdateRichTextBox(string.Format("CAPTCHA {0} uploaded: {1}", captchaFileName, captcha.Id));

                // Poll for the CAPTCHA status until it's solved.
                while (captcha.Uploaded && !captcha.Solved)
                {
                    System.Threading.Thread.Sleep(Client.PollsInterval * 1000);
                    captcha = client.GetCaptcha(captcha.Id);
                }

                if (captcha.Solved)
                {
                    //Left for debugging purposes, flooding up the richtextBxo
                    //Console.WriteLine(string.Format("CAPTCHA {0} solved: {1}", captchaFileName, captcha.Text));                    
                }
                else
                {
                    //UpdateRichTextBox("CAPTCHA was not solved");
                }
            }
            else
            {
                //UpdateRichTextBox("CAPTCHA was not uploaded");
            }
            return captcha.Text;
        }

        private async Task<string> PostRequest(string ViewState, string EventValidation, string HipImage, string gcNum, string Captcha)
        {
            //Add all the correct Headers for the POST
            var keyValues = new List<KeyValuePair<string, string>>();
            keyValues.Add(new KeyValuePair<string, string>("__EVENTTARGET", "ctl00$BodyContent$cont"));
            keyValues.Add(new KeyValuePair<string, string>("__EVENTARGUMENT", ""));
            keyValues.Add(new KeyValuePair<string, string>("__VIEWSTATE", ViewState));
            keyValues.Add(new KeyValuePair<string, string>("__VIEWSTATEGENERATOR", "C37D1CEB"));
            keyValues.Add(new KeyValuePair<string, string>("__VIEWSTATEENCRYPTED", ""));
            keyValues.Add(new KeyValuePair<string, string>("__EVENTVALIDATION", EventValidation));
            keyValues.Add(new KeyValuePair<string, string>("ctl00$BodyContent$tbCardNumber", gcNum));
            //keyValues.Add(new KeyValuePair<string, string>("ctl00$BodyContent$ucCaptcha$ImageHipChallenge1$ctl00", HipImage));
            //keyValues.Add(new KeyValuePair<string, string>("ctl00$BodyContent$ucCaptcha$tbHipAlphaNumeric", Captcha));
            keyValues.Add(new KeyValuePair<string, string>("g-recaptcha-response", Captcha));
            keyValues.Add(new KeyValuePair<string, string>("ctl00$BodyContent$tbUserName", ""));
            keyValues.Add(new KeyValuePair<string, string>("ctl00$BodyContent$tbPassword", ""));

            var baseAddress = new Uri("https://www.paneracards.com");

            CookieContainer cookieContainer = new CookieContainer();
            using (HttpClientHandler handler = new HttpClientHandler() { CookieContainer = cookieContainer })

            using (System.Net.Http.HttpClient client = new System.Net.Http.HttpClient(handler))
            {
                //Add more headers and cookies for the request
                cookieContainer.Add(baseAddress, new Cookie("AspxAutoDetectCookieSupport", "1"));
                client.BaseAddress = baseAddress;
                client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json; charset=utf-8");
                client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
                client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/53.0.2785.116 Safari/537.36");
                client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate, br");
                client.DefaultRequestHeaders.Add("Connection", "keep-alive");
                client.DefaultRequestHeaders.Host = "www.paneracards.com";

                var request = new HttpRequestMessage(HttpMethod.Post, "/checkbalance.aspx");
                request.Content = new FormUrlEncodedContent(keyValues);

                using (HttpResponseMessage response = await client.SendAsync(request))
                {
                    using (HttpContent content = response.Content)
                    {
                        string mycontent = await content.ReadAsStringAsync();
                        return mycontent;
                    }
                }
            }
        }

        private async Task DownloadCaptcha(string file_name, string url)
        {
            try
            {
                using (WebClient client = new WebClient())
                {

                    client.DownloadFileCompleted += new AsyncCompletedEventHandler(client_DownloadFileCompleted);
                    await client.DownloadFileTaskAsync(new Uri(url), @"Captcha\" + file_name);
                }
            }
            catch (System.Exception e)
            {
                Console.Write("Failed to download Captcah: " + e.Message);
            }
        }

        //Really just used for debugging purposes
        private void client_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            Console.WriteLine("Finished downloading Captcha.");
        }

        public async Task<Tuple<string, string>> Start_Factory(string name, long gcNum)
        {
            string gcNumString = gcNum.ToString();
            //Make the initial request
            Form1._Form1.UpdateRichTextBox("Making request for: " + gcNumString);
            string requestContent = await GetRequest(name, gcNumString);

            //parse out the needed information
            string ViewState = GetElementById("__VIEWSTATE.+?value=\"(.+?)\"", requestContent);
            string EventValidation = GetElementById("__EVENTVALIDATION.+?value=\"(.+?)\"", requestContent);
            string HipImage = GetElementById("type=\"hidden\" value=\"(.+?)\"", requestContent);
            string pictureUrl = "https://www.paneracards.com" + "/controls/ImageHipChallenge.aspx?w=170&h=60&id=" + HipImage;

            //download the captcha
            Form1._Form1.UpdateRichTextBox("Downloading the captcha for: " + name);
            await DownloadCaptcha(string.Format("{0}.png", name), pictureUrl);

            //Wait to solve the captcha
            string captcha = await UploadCaptcha(name + ".png");

            //Lets make the Post and see
            Form1._Form1.UpdateRichTextBox("Retrieving Balance info for: " + name);
            string responseContent = await PostRequest(ViewState, EventValidation, HipImage, gcNumString, captcha);

            //lets parse out the balance
            string Balance = GetElementById("<span id=\"ctl00_BodyContent_lblBalance\"[^>]*>([^<]*)", responseContent);
            //UpdateRichTextBox("Gift Card Num: " + gcNum + " with BALANCE: " + Balance);

            return Tuple.Create(gcNumString, Balance);
        }
    }
}

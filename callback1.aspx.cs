using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Net;
using System.Text;
using System.IO;
using System.Dynamic;
using System.Web.Script.Serialization;
using System.Collections.ObjectModel;
using System.Collections;

public partial class callback1 : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        string client_id = "your client id";
        string client_secret = "your client secret";
        string redirect_uri = "your redirect uri";
        string token_uri = Session["tokenendp"].ToString(); //get in OpenIDOAuth.aspx
        string code = Request["code"].ToString();
        string access_token = "";
        string tokenstr = "";

        byte[] bhash = System.Text.Encoding.Default.GetBytes(client_id + ":" + client_secret);
        string hash = System.Convert.ToBase64String(bhash, 0, bhash.Length);

        byte[] content = System.Text.Encoding.Default.GetBytes("grant_type=authorization_code&code=" + code + "&redirect_uri=" + redirect_uri);

        HttpWebRequest webreq = HttpWebRequest.Create(token_uri) as HttpWebRequest;
        webreq.Method = "POST";
        webreq.ContentType = "application/x-www-form-urlencoded";
        webreq.Timeout = 30000;
        webreq.Headers.Add("Authorization", "Basic " + hash);
        webreq.PreAuthenticate = true;
        webreq.ContentLength = content.Length;

        using (Stream reqStream = webreq.GetRequestStream())
        {
            reqStream.Write(content, 0, content.Length);
        }

        //取得回應資料
        using (HttpWebResponse resp = webreq.GetResponse() as HttpWebResponse)
        {
            using (StreamReader sr = new StreamReader(resp.GetResponseStream()))
            {
                tokenstr = sr.ReadToEnd();
            }
        }

        //取得token
        JavaScriptSerializer j = new JavaScriptSerializer();
        j.RegisterConverters(new JavaScriptConverter[] { new DynamicJsonConverter() });
        dynamic jobj = j.DeserializeObject(tokenstr) as dynamic;
        foreach (var result in jobj)
        {
            if (result.Key == "access_token")
            {
                access_token = result.Value;
            }
        }
        
        //取得userinfo
        Response.Write(GetUserInfo(access_token));

    }

    protected string GetUserInfo(string access_token)
    {
        string targetUrl = Session["userendp"].ToString(); //get in OpenIDOAuth.aspx
        
        HttpWebRequest request1 = HttpWebRequest.Create(targetUrl) as HttpWebRequest;
        request1.Method = "GET";
        request1.ContentType = "application/json";
        request1.Timeout = 30000;
        request1.Headers.Add("Authorization", "Bearer " + access_token);
        request1.PreAuthenticate = true;

        string retstr = "";

        // 取得回應資料
        using (HttpWebResponse response1 = request1.GetResponse() as HttpWebResponse)
        {
            using (StreamReader sr = new StreamReader(response1.GetResponseStream()))
            {
                retstr = sr.ReadToEnd();
            }
        }

        return retstr;
    }
}

public class DynamicJsonConverter : JavaScriptConverter
{
    public override object Deserialize(IDictionary<string, object> dictionary, Type type, JavaScriptSerializer serializer)
    {
        if (dictionary == null)
            throw new ArgumentNullException("dictionary");

        if (type == typeof(object))
        {
            return new DynamicJsonObject(dictionary);
        }
        return null;
    }

    public override IDictionary<string, object> Serialize(object obj, JavaScriptSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override IEnumerable<Type> SupportedTypes
    {
        get { return new ReadOnlyCollection<Type>(new List<Type>(new Type[] { typeof(object) })); }
    }

}

public class DynamicJsonObject : DynamicObject
{
    public IDictionary<string, object> Dictionary { get; set; }

    public DynamicJsonObject(IDictionary<string, object> dictionary)
    {
        this.Dictionary = dictionary;
    }

    public override bool TryGetMember(GetMemberBinder binder, out object result)
    {
        result = this.Dictionary[binder.Name];

        if (result is IDictionary<string, object>)
        {
            result = new DynamicJsonObject(result as IDictionary<string, object>);
        }
        else if (result is ArrayList && (result as ArrayList) is IDictionary<string, object>)
        {
            result = new List<DynamicJsonObject>((result as ArrayList).ToArray().Select(x => new DynamicJsonObject(x as IDictionary<string, object>)));
        }
        else if (result is ArrayList)
        {
            result = new List<object>((result as ArrayList).ToArray());
        }
        else if (result is ArrayList)
        {
            var list = new List<object>();
            foreach (var o in (result as ArrayList).ToArray())
            {
                if (o is IDictionary<string, object>)
                {
                    list.Add(new DynamicJsonObject(o as IDictionary<string, object>));
                }
                else
                {
                    list.Add(o);
                }
            }
            result = list;
        }

        return this.Dictionary.ContainsKey(binder.Name);
    }
}
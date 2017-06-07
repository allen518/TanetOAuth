using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Net;
using System.IO;
using System.Dynamic;
using System.Web.Script.Serialization;
using System.Collections.ObjectModel;
using System.Collections;

public partial class Web_OpenID_OpenIDOAuth : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        //test tanet oidc
        string client_id = "your client id";
        string redirect_uri = "your redirect uri";       
        string authendp = "";

        Session["state"] = DateTime.Now.Ticks.ToString();

        string authendpjson = GetAuthPoint();

        JavaScriptSerializer j = new JavaScriptSerializer();
        j.RegisterConverters(new JavaScriptConverter[] { new DynamicJsonConverter() });
        dynamic jobj = j.DeserializeObject(authendpjson) as dynamic;
        foreach (var result in jobj)
        {
            if (result.Key == "authorization_endpoint")
            {
                authendp = result.Value;
            }

            if (result.Key == "userinfo_endpoint")
            {
                Session["userendp"] = result.Value;
            }

            if (result.Key == "token_endpoint")
            {
                Session["tokenendp"] = result.Value;
            }
        }

        string targetUrl = authendp + "?response_type=code&client_id=" + client_id  + "&redirect_uri=" + redirect_uri + "&scope=openid+email+profile&state=" + Session["state"] + "&nonce=" + DateTime.Now.Ticks.ToString();
        Response.Redirect(targetUrl);
        
        
    }

    //取得Auth Endpoint
    protected string GetAuthPoint()
    {
        string targetUrl = "https://oidc.tanet.edu.tw/.well-known/openid-configuration";

        HttpWebRequest request1 = HttpWebRequest.Create(targetUrl) as HttpWebRequest;
        request1.Method = "GET";
        request1.ContentType = "application/json";
        request1.Timeout = 30000;

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
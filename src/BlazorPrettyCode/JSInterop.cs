using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace BlazorPrettyCode
{
    class JSInterop
    {
        public static Task<string> GetAndHide(string id)
        {
            return JSRuntime.Current.InvokeAsync<string>("blazorPrettyCode.getAndHide", id);
        }
    }
}

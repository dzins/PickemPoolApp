using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(PickemPoolApp.Startup))]
namespace PickemPoolApp
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}

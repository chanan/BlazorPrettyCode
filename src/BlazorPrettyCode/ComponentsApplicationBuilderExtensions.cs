using BlazorStyled;
using Microsoft.AspNetCore.Components.Builder;

namespace BlazorPrettyCode
{
    public static class ComponentsApplicationBuilderExtensions
    {
        public static IComponentsApplicationBuilder AddClientSidePrettyCode(this IComponentsApplicationBuilder componentsApplicationBuilder)
        {
            componentsApplicationBuilder.AddComponent<ClientSideStyled>("#styled");
            return componentsApplicationBuilder;
        }
    }
}

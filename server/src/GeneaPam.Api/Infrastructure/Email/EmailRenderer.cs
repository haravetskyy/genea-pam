using RazorLight;

namespace GeneaPam.Api.Infrastructure.Email;

public sealed class EmailRenderer(IRazorLightEngine engine)
{
    public async Task<string> RenderAsync<TModel>(string templateKey, TModel model)
        where TModel : class => await engine.CompileRenderAsync(templateKey, model);
}

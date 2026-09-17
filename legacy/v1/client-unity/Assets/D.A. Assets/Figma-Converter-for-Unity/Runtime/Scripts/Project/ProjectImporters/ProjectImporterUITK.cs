using DA_Assets.FCU.Model;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DA_Assets.FCU
{
    public sealed class ProjectImporterUITK : ProjectImporterBase
    {
        protected override async Task RunImportAsync(string[] frameIds, CancellationToken token)
        {
            await ImportFrame(token, frameIds);
        }

        protected override Task ShowPreImportWindow(CancellationToken token)
        {
            return Task.CompletedTask;
        }

        protected override Task LoadPrefabs(CancellationToken token)
        {
            return Task.CompletedTask;
        }

        protected override Task DrawGameObjects(FObject virtualPage, CancellationToken token)
        {
            return Task.CompletedTask;
        }

        protected override async Task FinalSteps(FObject virtualPage, List<FObject> currentPage, CancellationToken token)
        {
            await monoBeh.NameSetter.Set_UITK_Names(currentPage);

#if FCU_EXISTS && FCU_UITK_EXT_EXISTS
            await monoBeh.UITK_Converter.Convert(virtualPage, currentPage, token);
#endif
        }
    }
}
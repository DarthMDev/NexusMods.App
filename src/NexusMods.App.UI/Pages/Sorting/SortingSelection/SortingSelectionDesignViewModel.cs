using System.Collections.ObjectModel;
using NexusMods.Abstractions.UI;

namespace NexusMods.App.UI.Pages.Sorting;

public class SortingSelectionDesignViewModel : AViewModel<ISortingSelectionViewModel>, ISortingSelectionViewModel
{
    public ReadOnlyObservableCollection<ILoadOrderViewModel> LoadOrderViewModels { get; }

    public SortingSelectionDesignViewModel()
    {
        var loadOrderViewModels = new ObservableCollection<ILoadOrderViewModel>
        {
            new LoadOrderDesignViewModel { SortOrderName = "Load order (RedMOD)", 
                InfoAlertHeading = "Load Order for REDmod files in Cyberpunk 2077 - First Loaded Wins",
                InfoAlertMessage = "Some Cyberpunk 2077 mods use REDmod files to alter core gameplay elements. If two REDmod files modify the same part of the game, the one loaded first will take priority and overwrite changes from those loaded later.\n\nFor example, the 1st position overwrites the 2nd, the 2nd overwrites the 3rd, and so on." 
            },
            new LoadOrderDesignViewModel { SortOrderName = "Load Order (Archive XL)" },
            new LoadOrderDesignViewModel { SortOrderName = "File Overwrites" }
        };
        
        LoadOrderViewModels = new ReadOnlyObservableCollection<ILoadOrderViewModel>(loadOrderViewModels);
    }
}
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Disposables;
using Avalonia.Controls.Models.TreeDataGrid;
using DynamicData;
using DynamicData.Binding;
using NexusMods.Abstractions.Settings;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Controls.Alerts;
using NexusMods.App.UI.Settings;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.Sorting;

public class LoadOrderDesignViewModel : AViewModel<ILoadOrderViewModel>, ILoadOrderViewModel
{
    public TreeDataGridAdapter<ILoadOrderItemModel, Guid> Adapter { get; set; }
    public string SortOrderName { get; set; } = "Sort Order Name";
    public string InfoAlertTitle { get; set;} = "Info Alert Title";
    public string InfoAlertHeading { get; set;} = "Info Alert Heading";
    public string InfoAlertMessage { get; set;} = "Info Alert Message";
    public bool InfoAlertIsVisible { get; set; } = true;
    public ReactiveCommand<Unit, Unit> InfoAlertCommand { get; } = ReactiveCommand.Create(() => { });
    public string TrophyToolTip { get; set;} = "Trophy Tool Tip";
    public ListSortDirection SortDirectionCurrent { get; set; }
    public bool IsWinnerTop { get; set;}
    public string EmptyStateMessageTitle { get; } = "Empty State Message Title";
    public string EmptyStateMessageContents { get; } = "Empty State Message Contents";
    public AlertSettingsWrapper AlertSettingsWrapper { get; }

    public LoadOrderDesignViewModel(ISettingsManager settingsManager)
    {
        Adapter = new LoadOrderTreeDataGridDesignAdapter();

        this.WhenActivated(d =>
            {
                Adapter.Activate();
                Disposable.Create(() => Adapter.Deactivate())
                    .DisposeWith(d);
            }
        );
        
        AlertSettingsWrapper = new AlertSettingsWrapper(settingsManager, "cyberpunk2077 redmod load-order first-loaded-wins");
    }
}


// adapter used for design view, based on the actual adapter LoadOrderViewModel.LoadOrderTreeDataGridAdapter 
public class LoadOrderTreeDataGridDesignAdapter : TreeDataGridAdapter<ILoadOrderItemModel, Guid>
{
    protected override IObservable<IChangeSet<ILoadOrderItemModel, Guid>> GetRootsObservable(bool viewHierarchical)
    {
        var items = new ObservableCollection<ILoadOrderItemModel>([
                new LoadOrderItemDesignModel() { DisplayName = "Item 1", Guid = Guid.NewGuid(), SortIndex = 0 },
                new LoadOrderItemDesignModel() { DisplayName = "Item 2", Guid = Guid.NewGuid(), SortIndex = 1 },
            ]
        );
        
        return items.ToObservableChangeSet(item => ((LoadOrderItemDesignModel)item).Guid);
    }

    protected override IColumn<ILoadOrderItemModel>[] CreateColumns(bool viewHierarchical)
    {
        return
        [
            // TODO: Use <see cref="ColumnCreator"/> to create the columns using interfaces
            new HierarchicalExpanderColumn<ILoadOrderItemModel>(
                inner: LoadOrderTreeDataGridAdapter.CreateIndexColumn("Index"),
                childSelector: static model => model.Children,
                hasChildrenSelector: static model => model.HasChildren.Value,
                isExpandedSelector: static model => model.IsExpanded
            )
            {
                Tag = "expander",
            },
            LoadOrderTreeDataGridAdapter.CreateNameColumn("Name"),
        ];
    }
}

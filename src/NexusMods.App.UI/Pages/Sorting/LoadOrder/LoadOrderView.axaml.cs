using System.ComponentModel;
using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Helpers;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.Sorting;

public partial class LoadOrderView : ReactiveUserControl<ILoadOrderViewModel>
{
    public LoadOrderView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
            {
                TreeDataGridViewHelper.SetupTreeDataGridAdapter<LoadOrderView, ILoadOrderViewModel, ILoadOrderItemModel, Guid>(
                    this,
                    SortOrderTreeDataGrid,
                    vm => vm.Adapter
                );

                this.OneWayBind(ViewModel,
                        vm => vm.Adapter.Source.Value,
                        view => view.SortOrderTreeDataGrid.Source
                    )
                    .DisposeWith(disposables);
                
                this.WhenAnyValue(view => view.ViewModel!.SortDirectionCurrent)
                    .Subscribe(sortCurrentDirection =>
                        {
                            var isAscending = sortCurrentDirection == ListSortDirection.Ascending;
                            ArrowUpIcon.IsVisible = !isAscending;
                            ArrowDownIcon.IsVisible = isAscending;
                        }
                    )
                    .DisposeWith(disposables);
                
                this.WhenAnyValue(view => view.ViewModel!.IsWinnerTop)
                    .Subscribe(isWinnerTop =>
                        {
                            DockPanel.SetDock(TrophyIcon, isWinnerTop ? Dock.Top : Dock.Bottom);
                            TrophyBarPanel.Classes.Add(isWinnerTop ? "IsWinnerTop" : "IsWinnerBottom");
                        }
                    )
                    .DisposeWith(disposables);
                
                // empty state
                this.OneWayBind(ViewModel, 
                        vm => vm.Adapter.IsSourceEmpty.Value, 
                        view => view.EmptyState.IsActive)
                    .DisposeWith(disposables);
                
                // alert
                this.OneWayBind(ViewModel, vm => vm.AlertSettingsWrapper, view => view.LoadOrderAlert.AlertSettings)
                    .DisposeWith(disposables);
            }
        );
    }

    private void OnRowDrop(object? sender, TreeDataGridRowDragEventArgs e)
    {
        // NOTE(Al12rs): This is important in case the source is read-only, otherwise TreeDataGrid will attempt to
        // move the items, updating the source collection, throwing an exception in the process.
        e.Handled = true;
    }
}

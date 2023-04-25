﻿using System.Reactive.Disposables;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Platform.Storage;
using Avalonia.ReactiveUI;
using NexusMods.App.UI.RightContent.LoadoutGrid.Columns;
using ReactiveUI;

namespace NexusMods.App.UI.RightContent.LoadoutGrid;

public partial class LoadoutGridView : ReactiveUserControl<ILoadoutGridViewModel>
{
    public LoadoutGridView()
    {
        InitializeComponent();
        this.WhenActivated(d =>
        {
            this.WhenAnyValue(view => view.ViewModel!.Mods)
                .BindToUi(this, view => view.ModsDataGrid.Items)
                .DisposeWith(d);

            this.WhenAnyValue(view => view.ViewModel!.LoadoutName)
                .BindToUi(this, view => view.ModlistNameText.Text)
                .DisposeWith(d);

            AddModButton.Command =
                ReactiveCommand.CreateFromTask(AddMod);

            this.WhenAnyValue(view => view.ViewModel!.Columns)
                .OnUI()
                .Subscribe(columns =>
                {
                    ModsDataGrid.Columns.Clear();
                    foreach (var column in columns)
                    {
                        var generatedColumn = column.Generate();
                        generatedColumn.Header = column.Type.GenerateHeader();
                        ModsDataGrid.Columns.Add(generatedColumn);
                    }
                })
                .DisposeWith(d);
        });
    }
    
    private async Task AddMod()
    {
        var provider = TopLevel.GetTopLevel(this)!.StorageProvider;
        var options =
            new FilePickerOpenOptions
            {
                Title = "Please select a mod file to install.",
                AllowMultiple = true,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("ZIP") {Patterns = new [] {"*.zip"}},
                    new FilePickerFileType("RAR") {Patterns = new [] {"*.rar"}},
                    new FilePickerFileType("7-Zip") {Patterns = new [] {"*.7z"}},
                }
            };

        foreach (var file in await provider.OpenFilePickerAsync(options))
        {
            await ViewModel!.AddMod(file.Path.LocalPath);
        }
    }
}

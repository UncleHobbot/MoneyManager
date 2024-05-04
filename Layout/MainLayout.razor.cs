﻿using Microsoft.AspNetCore.Components;

namespace MoneyManager.Layout;

public partial class MainLayout : LayoutComponentBase
{
    [Inject] private SettingsService service { get; set; }
    [Inject] private Seed seed { get; set; }

    private SettingsModel data = new();

    protected override async Task OnInitializedAsync()
    {
        data = await service.GetSettings();
        await seed.SeedData();
    }
}
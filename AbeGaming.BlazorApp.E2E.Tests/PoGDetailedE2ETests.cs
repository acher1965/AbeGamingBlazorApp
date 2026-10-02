using Microsoft.Playwright;

namespace AbeGaming.BlazorApp.E2E.Tests
{
    public class PoGDetailedE2ETests
    {
        private static string BaseUrl =>
            Environment.GetEnvironmentVariable("E2E_BASE_URL")?.TrimEnd('/')
            ?? "http://localhost:5211";

        [Fact]
        public async Task DetailedUnits_DefaultSetup_ShowsStrengthsAndDetailedStats()
        {
            using IPlaywright playwright = await Playwright.CreateAsync();
            await using IBrowser browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            IPage page = await browser.NewPageAsync();
            IResponse? response = await page.GotoAsync($"{BaseUrl}/pogbattle", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 30000,
            });
            Assert.True(response!.Ok);

            await page.CheckAsync("#pogDetailedUnits");

            // Three full GE Armies (3 x 5) attack three full FR Armies (3 x 3).
            Assert.Contains("Combat Strength 15", await page.TextContentAsync("#pogAttackerSummary") ?? string.Empty);
            Assert.Contains("Combat Strength 9", await page.TextContentAsync("#pogDefenderSummary") ?? string.Empty);
            Assert.True(await page.IsCheckedAsync("#pogAttackerUnit2Reserve"));
            Assert.True(await page.IsCheckedAsync("#pogDefenderUnit2Reserve"));

            await page.ClickAsync("#pogCalculateExactStats");
            await page.WaitForSelectorAsync("#pogDetailedStats", new PageWaitForSelectorOptions { Timeout = 10000 });
            Assert.Contains("Defender steps lost", await page.TextContentAsync("#resultsSection") ?? string.Empty);

            await page.ClickAsync("#pogRollSingleBattle");
            await page.WaitForSelectorAsync("#pogDetailedResult", new PageWaitForSelectorOptions { Timeout = 10000 });
        }

        [Fact]
        public async Task DetailedUnits_AddingAUnitUpdatesTheStrength()
        {
            using IPlaywright playwright = await Playwright.CreateAsync();
            await using IBrowser browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            IPage page = await browser.NewPageAsync();
            await page.GotoAsync($"{BaseUrl}/pogbattle", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 30000 });

            await page.CheckAsync("#pogDetailedUnits");
            Assert.Equal(0, await page.Locator("#pogDefenderAddUnit").CountAsync()); // three defenders is the limit
            await page.ClickAsync("#pogAttackerAddUnit"); // adds a GE Army (5)

            // Blazor re-renders asynchronously after the click; wait for the new strength (15 + 5).
            await page.WaitForSelectorAsync("#pogAttackerUnit3Type", new PageWaitForSelectorOptions { Timeout = 5000 });
            await page.WaitForFunctionAsync(
                "() => document.querySelector('#pogAttackerSummary').textContent.includes('Combat Strength 20')",
                null, new PageWaitForFunctionOptions { Timeout = 5000 });
        }

        [Fact]
        public async Task DetailedUnits_AlliedAttacker_SwapsTheSidesUnitLists()
        {
            using IPlaywright playwright = await Playwright.CreateAsync();
            await using IBrowser browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            IPage page = await browser.NewPageAsync();
            await page.GotoAsync($"{BaseUrl}/pogbattle", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 30000 });

            await page.CheckAsync("#pogDetailedUnits");
            Assert.Equal(0, await page.Locator("#pogAttackerUnit0Type option[value='FR_ARMY']").CountAsync());

            await page.SelectOptionAsync("#pogAttackerFaction", "AlliedPowers");

            await page.WaitForFunctionAsync(
                "() => document.querySelector('#pogAttackerSummary').textContent.includes('Combat Strength 9')",
                null, new PageWaitForFunctionOptions { Timeout = 5000 });
            Assert.Contains("Combat Strength 15", await page.TextContentAsync("#pogDefenderSummary") ?? string.Empty);
            Assert.Equal(0, await page.Locator("#pogAttackerUnit0Type option[value='GE_ARMY']").CountAsync());
            Assert.Equal(1, await page.Locator("#pogDefenderUnit0Type option[value='GE_ARMY']").CountAsync());
        }

        [Fact]
        public async Task DetailedUnits_OnALandscapePhone_SidebarGivesWayToTheMenuButton()
        {
            using IPlaywright playwright = await Playwright.CreateAsync();
            await using IBrowser browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            IPage page = await browser.NewPageAsync(new BrowserNewPageOptions { ViewportSize = new ViewportSize { Width = 844, Height = 390 } });
            await page.GotoAsync($"{BaseUrl}/pogbattle", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 30000 });

            Assert.True(await page.IsVisibleAsync(".nav-scrollable"));
            Assert.False(await page.IsVisibleAsync(".navbar-toggler"));

            await page.CheckAsync("#pogDetailedUnits");
            await page.WaitForSelectorAsync(".page.compact-nav-landscape", new PageWaitForSelectorOptions { Timeout = 5000 });
            Assert.False(await page.IsVisibleAsync(".nav-scrollable"));
            Assert.True(await page.IsVisibleAsync(".navbar-toggler"));

            // The menu button still opens the navigation.
            await page.ClickAsync(".navbar-toggler");
            await page.WaitForSelectorAsync(".nav-scrollable", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });

            // A desktop window keeps its sidebar.
            await page.SetViewportSizeAsync(1280, 800);
            await page.GotoAsync($"{BaseUrl}/pogbattle", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 30000 });
            await page.CheckAsync("#pogDetailedUnits");
            await page.WaitForSelectorAsync(".page.compact-nav-landscape", new PageWaitForSelectorOptions { Timeout = 5000 });
            Assert.True(await page.IsVisibleAsync(".nav-scrollable"));
        }
    }
}

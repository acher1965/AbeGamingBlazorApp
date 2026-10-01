using Microsoft.Playwright;

namespace AbeGaming.BlazorApp.E2E.Tests
{
    public class PoGDetailedE2ETests
    {
        private static string BaseUrl =>
            Environment.GetEnvironmentVariable("E2E_BASE_URL")?.TrimEnd('/')
            ?? "http://localhost:5211";

        [Fact]
        public async Task DetailedUnits_DefaultTannenbergSetup_ShowsStrengthsAndDetailedStats()
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

            // GE Army (5) + GE Corps (2) against a RU Army (3).
            Assert.Contains("7", await page.TextContentAsync("#pogAttackerSummary") ?? string.Empty);
            Assert.Contains("3", await page.TextContentAsync("#pogDefenderSummary") ?? string.Empty);

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
            await page.ClickAsync("#pogDefenderAddUnit"); // adds a FR Army (3)

            // Blazor re-renders asynchronously after the click; wait for the new strength (3 + 3).
            await page.WaitForSelectorAsync("#pogDefenderUnit1Type", new PageWaitForSelectorOptions { Timeout = 5000 });
            await page.WaitForFunctionAsync(
                "() => document.querySelector('#pogDefenderSummary').textContent.includes('Combat Strength 6')",
                null, new PageWaitForFunctionOptions { Timeout = 5000 });
        }
    }
}

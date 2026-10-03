using Microsoft.Playwright;

namespace AbeGaming.BlazorApp.E2E.Tests
{
    public class TnwSiegeE2ETests
    {
        private static string BaseUrl =>
            Environment.GetEnvironmentVariable("E2E_BASE_URL")?.TrimEnd('/')
            ?? "http://localhost:5211";

        [Fact]
        public async Task DefaultSetup_BattleRatingStartsAtOne()
        {
            using IPlaywright playwright = await Playwright.CreateAsync();
            await using IBrowser browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            IPage page = await browser.NewPageAsync();
            await page.GotoAsync($"{BaseUrl}/tnwsiege", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 30000 });

            Assert.Equal("1", await page.InputValueAsync("#tnwSiegeCommanderBattleRating"));
        }

        [Fact]
        public async Task ExactStats_RendersFallProbability()
        {
            using IPlaywright playwright = await Playwright.CreateAsync();
            await using IBrowser browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
            });

            IPage page = await browser.NewPageAsync();
            IResponse? response = await page.GotoAsync($"{BaseUrl}/tnwsiege", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 30000,
            });

            Assert.NotNull(response);
            Assert.True(response!.Ok, $"Could not load {BaseUrl}/tnwsiege (HTTP {(int)response.Status})");

            await page.ClickAsync("#tnwSiegeCalculateExactStats");
            await page.WaitForSelectorAsync("text=Exact Stats", new PageWaitForSelectorOptions { Timeout = 10000 });

            string bodyText = await page.TextContentAsync("#resultsSection") ?? string.Empty;
            Assert.Contains("Fortress Falls", bodyText);
            Assert.Contains("Overrun", bodyText);
        }

        [Fact]
        public async Task UnitsInput_ClampsToUpperBound()
        {
            using IPlaywright playwright = await Playwright.CreateAsync();
            await using IBrowser browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
            });

            IPage page = await browser.NewPageAsync();
            await page.GotoAsync($"{BaseUrl}/tnwsiege", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 30000,
            });

            await page.FillAsync("#tnwSiegeUnits", "999");
            await page.PressAsync("#tnwSiegeUnits", "Tab");

            string unitsValue = await page.InputValueAsync("#tnwSiegeUnits");
            Assert.Equal("30", unitsValue);
        }

        [Fact]
        public async Task RollOnce_RendersASingleSiegeResult()
        {
            using IPlaywright playwright = await Playwright.CreateAsync();
            await using IBrowser browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
            });

            IPage page = await browser.NewPageAsync();
            await page.GotoAsync($"{BaseUrl}/tnwsiege", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 30000,
            });

            await page.ClickAsync("#tnwSiegeRollOnce");
            await page.WaitForSelectorAsync("text=Single Siege Result", new PageWaitForSelectorOptions { Timeout = 10000 });

            string bodyText = await page.TextContentAsync("#resultsSection") ?? string.Empty;
            Assert.Contains("Rounds:", bodyText);
        }
    }
}

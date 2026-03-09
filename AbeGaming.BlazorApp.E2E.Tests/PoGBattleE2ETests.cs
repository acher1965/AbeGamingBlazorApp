using Microsoft.Playwright;

namespace AbeGaming.BlazorApp.E2E.Tests
{
    public class PoGBattleE2ETests
    {
        private static string BaseUrl =>
            Environment.GetEnvironmentVariable("E2E_BASE_URL")?.TrimEnd('/')
            ?? "http://localhost:5211";

        [Fact]
        public async Task TrenchInput_ClampsToTwo()
        {
            using IPlaywright playwright = await Playwright.CreateAsync();
            await using IBrowser browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
            });

            IPage page = await browser.NewPageAsync();
            IResponse? response = await page.GotoAsync($"{BaseUrl}/pogbattle", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 30000,
            });

            Assert.NotNull(response);
            Assert.True(response!.Ok, $"Could not load {BaseUrl}/pogbattle (HTTP {(int)response.Status})");

            await page.FillAsync("#pogTrench", "9");
            await page.PressAsync("#pogTrench", "Tab");

            string trenchValue = await page.InputValueAsync("#pogTrench");
            Assert.Equal("2", trenchValue);
        }

        [Fact]
        public async Task AttackerOos_DisablesCalculationButtons()
        {
            using IPlaywright playwright = await Playwright.CreateAsync();
            await using IBrowser browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
            });

            IPage page = await browser.NewPageAsync();
            await page.GotoAsync($"{BaseUrl}/pogbattle", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 30000,
            });

            await page.CheckAsync("#pogAttackerOOS");

            bool exactDisabled = await page.IsDisabledAsync("#pogCalculateExactStats");
            bool rollDisabled = await page.IsDisabledAsync("#pogRollSingleBattle");
            string warningText = await page.TextContentAsync(".alert-warning") ?? string.Empty;

            Assert.True(exactDisabled);
            Assert.True(rollDisabled);
            Assert.Contains("OOS units may not attack", warningText);
        }

        [Fact]
        public async Task ExactStats_RendersStatsCard()
        {
            using IPlaywright playwright = await Playwright.CreateAsync();
            await using IBrowser browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
            });

            IPage page = await browser.NewPageAsync();
            await page.GotoAsync($"{BaseUrl}/pogbattle", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 30000,
            });

            await page.ClickAsync("#pogCalculateExactStats");
            await page.WaitForSelectorAsync("text=Exact Stats", new PageWaitForSelectorOptions { Timeout = 10000 });

            string bodyText = await page.TextContentAsync("#resultsSection") ?? string.Empty;
            Assert.Contains("Attacker", bodyText);
            Assert.Contains("Defender", bodyText);
            Assert.Contains("Draw", bodyText);
        }
    }
}

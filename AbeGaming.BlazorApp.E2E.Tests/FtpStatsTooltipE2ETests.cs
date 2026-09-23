using Microsoft.Playwright;

namespace AbeGaming.BlazorApp.E2E.Tests
{
    public class FtpStatsTooltipE2ETests
    {
        private static string BaseUrl =>
            Environment.GetEnvironmentVariable("E2E_BASE_URL")?.TrimEnd('/')
            ?? "http://localhost:5211";

        [Fact]
        public async Task ExactStats_AttackerStaysIcon_HasTooltipAndShowsPopoverOnTap()
        {
            using IPlaywright playwright = await Playwright.CreateAsync();
            await using IBrowser browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
            });

            IPage page = await browser.NewPageAsync();
            await page.GotoAsync($"{BaseUrl}/ftpbattle", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 30000,
            });

            // 9 vs 5 SP gives a non-zero probability that the attacker stays.
            await page.FillAsync("#attackerSp", "9");
            await page.PressAsync("#attackerSp", "Tab");
            await page.FillAsync("#defenderSp", "5");
            await page.PressAsync("#defenderSp", "Tab");
            await page.ClickAsync("text=Calculate Exact Stats");

            ILocator staysTrigger = page.Locator("#resultsSection .info-tip-content[title^='Attacker stays']").First;
            await staysTrigger.WaitForAsync(new LocatorWaitForOptions { Timeout = 10000 });

            string? title = await staysTrigger.GetAttributeAsync("title");
            Assert.NotNull(title);
            Assert.Contains("does not retreat", title);

            await staysTrigger.ClickAsync();
            ILocator popover = page.Locator("#resultsSection .info-popover");
            await popover.WaitForAsync(new LocatorWaitForOptions { Timeout = 5000 });
            Assert.Equal(title, await popover.TextContentAsync());
        }
    }
}

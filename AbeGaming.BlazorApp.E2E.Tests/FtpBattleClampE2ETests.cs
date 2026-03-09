using Microsoft.Playwright;

namespace AbeGaming.BlazorApp.E2E.Tests
{
    public class FtpBattleClampE2ETests
    {
        private static string BaseUrl =>
            Environment.GetEnvironmentVariable("E2E_BASE_URL")?.TrimEnd('/')
            ?? "http://localhost:5211";

        [Fact]
        public async Task AmphibiousRules_ClampAttackerAndDefenderSpWhenRulesChange()
        {
            using IPlaywright playwright = await Playwright.CreateAsync();
            await using IBrowser browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
            });

            IPage page = await browser.NewPageAsync();
            IResponse? response = await page.GotoAsync($"{BaseUrl}/ftpbattle", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 30000,
            });

            Assert.NotNull(response);
            Assert.True(response!.Ok, $"Could not load {BaseUrl}/ftpbattle (HTTP {(int)response.Status})");

            // Attacker clamp flow: amphibious + army move on allows 9, turning army move off clamps to 3.
            await page.CheckAsync("#isAmphibious");
            await page.UncheckAsync("#isArmyMove");

            // Regression: entering above max in non-army amphibious must snap back to 3.
            await page.FillAsync("#attackerSp", "5");
            await page.PressAsync("#attackerSp", "Tab");

            string attackerSpAfterDirectEdit = await page.InputValueAsync("#attackerSp");
            Assert.Equal("3", attackerSpAfterDirectEdit);

            await page.CheckAsync("#isArmyMove");
            await page.FillAsync("#attackerSp", "9");
            await page.PressAsync("#attackerSp", "Tab");
            await page.UncheckAsync("#isArmyMove");

            string attackerSp = await page.InputValueAsync("#attackerSp");
            Assert.Equal("3", attackerSp);

            // Defender clamp flow: amphibious+fort allows 0, removing fort clamps back to 1.
            await page.CheckAsync("#fortPresent");
            await page.FillAsync("#defenderSp", "0");
            await page.PressAsync("#defenderSp", "Tab");
            await page.UncheckAsync("#fortPresent");

            string defenderSp = await page.InputValueAsync("#defenderSp");
            Assert.Equal("1", defenderSp);
        }
    }
}

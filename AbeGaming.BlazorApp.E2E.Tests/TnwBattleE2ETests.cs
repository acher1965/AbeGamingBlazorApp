using Microsoft.Playwright;

namespace AbeGaming.BlazorApp.E2E.Tests
{
    public class TnwBattleE2ETests
    {
        private static string BaseUrl =>
            Environment.GetEnvironmentVariable("E2E_BASE_URL")?.TrimEnd('/')
            ?? "http://localhost:5211";

        private static async Task<IPage> OpenPage(IBrowser browser)
        {
            IPage page = await browser.NewPageAsync();
            IResponse? response = await page.GotoAsync($"{BaseUrl}/tnwbattle", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 30000,
            });
            Assert.NotNull(response);
            Assert.True(response!.Ok, $"Could not load {BaseUrl}/tnwbattle (HTTP {(int)response.Status})");
            return page;
        }

        private static async Task SetUnits(IPage page, int attacker, int defender)
        {
            await page.FillAsync("#tnwAttackerUnits", attacker.ToString());
            await page.PressAsync("#tnwAttackerUnits", "Tab");
            await page.FillAsync("#tnwDefenderUnits", defender.ToString());
            await page.PressAsync("#tnwDefenderUnits", "Tab");
        }

        [Fact]
        public async Task DefaultSetup_IsNamedLandBattle_TwoUnitsNoCommanderEachSide()
        {
            using IPlaywright playwright = await Playwright.CreateAsync();
            await using IBrowser browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            IPage page = await OpenPage(browser);

            Assert.Contains("Land Battle", await page.TitleAsync());
            Assert.Contains("Land Battle", await page.TextContentAsync("h1") ?? string.Empty);
            Assert.Equal("2", await page.InputValueAsync("#tnwAttackerUnits"));
            Assert.Equal("2", await page.InputValueAsync("#tnwDefenderUnits"));
            Assert.Equal("0", await page.InputValueAsync("#tnwAttackerCommander"));
            Assert.Equal("0", await page.InputValueAsync("#tnwDefenderCommander"));
        }

        [Fact]
        public async Task SmallBattle_RendersExactStats()
        {
            using IPlaywright playwright = await Playwright.CreateAsync();
            await using IBrowser browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            IPage page = await OpenPage(browser);

            await page.ClickAsync("#tnwCalculateStats");
            await page.WaitForSelectorAsync("text=Exact Stats", new PageWaitForSelectorOptions { Timeout = 15000 });

            string bodyText = await page.TextContentAsync("#resultsSection") ?? string.Empty;
            Assert.Contains("Attacker Wins", bodyText);
            Assert.Contains("Defender Wins", bodyText);
        }

        [Fact]
        public async Task LargeBattle_FallsBackToMonteCarlo()
        {
            using IPlaywright playwright = await Playwright.CreateAsync();
            await using IBrowser browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            IPage page = await OpenPage(browser);
            await SetUnits(page, 20, 20);

            await page.ClickAsync("#tnwCalculateStats");
            await page.WaitForSelectorAsync("text=Monte Carlo Stats", new PageWaitForSelectorOptions { Timeout = 30000 });
        }

        [Fact]
        public async Task RollOnce_RendersASingleBattleResult()
        {
            using IPlaywright playwright = await Playwright.CreateAsync();
            await using IBrowser browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            IPage page = await OpenPage(browser);

            await page.ClickAsync("#tnwRollOnce");
            await page.WaitForSelectorAsync("text=Single Battle Result", new PageWaitForSelectorOptions { Timeout = 10000 });

            string bodyText = await page.TextContentAsync("#resultsSection") ?? string.Empty;
            Assert.Contains("Round 1:", bodyText);
        }

        [Fact]
        public async Task AmphibiousLanding_DisablesAndClearsTerrain()
        {
            using IPlaywright playwright = await Playwright.CreateAsync();
            await using IBrowser browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            IPage page = await OpenPage(browser);
            await page.SelectOptionAsync("#tnwTerrain", "Marsh");

            await page.SelectOptionAsync("#tnwAmphibiousLanding", "EnemyFortressPort");

            Assert.True(await page.IsDisabledAsync("#tnwTerrain"));
            Assert.Equal("None", await page.InputValueAsync("#tnwTerrain"));
        }

        [Fact]
        public async Task AmphibiousLanding_RollOnce_LogsShoreBatteriesAsRoundZero()
        {
            using IPlaywright playwright = await Playwright.CreateAsync();
            await using IBrowser browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            IPage page = await OpenPage(browser);
            await page.SelectOptionAsync("#tnwAmphibiousLanding", "EnemyFortressPort");

            await page.ClickAsync("#tnwRollOnce");
            await page.WaitForSelectorAsync("text=Single Battle Result", new PageWaitForSelectorOptions { Timeout = 10000 });

            string bodyText = await page.TextContentAsync("#resultsSection") ?? string.Empty;
            Assert.Contains("Shore batteries:", bodyText);
        }

        [Fact]
        public async Task NoPiecesOnASide_ShowsValidationError()
        {
            using IPlaywright playwright = await Playwright.CreateAsync();
            await using IBrowser browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            IPage page = await OpenPage(browser);
            await SetUnits(page, 0, 5);
            await page.SelectOptionAsync("#tnwAttackerCommander", "0");

            await page.ClickAsync("#tnwCalculateStats");

            await page.WaitForSelectorAsync("text=The attacker needs at least one Unit", new PageWaitForSelectorOptions { Timeout = 5000 });
        }
    }
}

using Microsoft.Playwright;

namespace AbeGaming.BlazorApp.E2E.Tests
{
    public class TnwNavalE2ETests
    {
        private static string BaseUrl =>
            Environment.GetEnvironmentVariable("E2E_BASE_URL")?.TrimEnd('/')
            ?? "http://localhost:5211";

        private static async Task<IPage> OpenPage(IBrowser browser)
        {
            IPage page = await browser.NewPageAsync();
            IResponse? response = await page.GotoAsync($"{BaseUrl}/tnwnaval", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 30000,
            });
            Assert.NotNull(response);
            Assert.True(response!.Ok, $"Could not load {BaseUrl}/tnwnaval (HTTP {(int)response.Status})");
            return page;
        }

        private static async Task SetUpRulebookExample(IPage page)
        {
            // Four British Squadrons vs two French and two Spanish (13.4).
            await page.FillAsync("#tnwActiveBritainSquadrons", "4");
            await page.PressAsync("#tnwActiveBritainSquadrons", "Tab");
            await page.FillAsync("#tnwInactiveFranceSquadrons", "2");
            await page.PressAsync("#tnwInactiveFranceSquadrons", "Tab");
            await page.FillAsync("#tnwInactiveSpainSquadrons", "2");
            await page.PressAsync("#tnwInactiveSpainSquadrons", "Tab");
        }

        [Fact]
        public async Task DefaultSetup_IsEmpty_NoEvasionFailureTickedByDefault()
        {
            using IPlaywright playwright = await Playwright.CreateAsync();
            await using IBrowser browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            IPage page = await OpenPage(browser);

            // Both Fleets start empty, so changing them takes no time clearing out a prefilled example.
            Assert.False(await page.IsCheckedAsync("#tnwActiveEvasionDie"));
            Assert.False(await page.IsCheckedAsync("#tnwInactiveEvasionDie"));
            Assert.Contains("0", await page.TextContentAsync("#tnwActiveRound1Dice") ?? string.Empty);
            Assert.Contains("0", await page.TextContentAsync("#tnwInactiveRound1Dice") ?? string.Empty);
        }

        [Fact]
        public async Task TickingEnemyFailedToEvade_ReproducesTheFullRulebookExample_13Versus6Dice()
        {
            using IPlaywright playwright = await Playwright.CreateAsync();
            await using IBrowser browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            IPage page = await OpenPage(browser);
            await SetUpRulebookExample(page);

            await page.CheckAsync("#tnwActiveEvasionDie");

            Assert.Contains("13", await page.TextContentAsync("#tnwActiveRound1Dice") ?? string.Empty);
            Assert.Contains("6", await page.TextContentAsync("#tnwInactiveRound1Dice") ?? string.Empty);
        }

        [Fact]
        public async Task Fischer_DisabledWithoutDanishSquadrons_EnabledOnceAddedAndIncreasesDice()
        {
            using IPlaywright playwright = await Playwright.CreateAsync();
            await using IBrowser browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            IPage page = await OpenPage(browser);
            await page.FillAsync("#tnwActiveBritainSquadrons", "4");
            await page.PressAsync("#tnwActiveBritainSquadrons", "Tab");

            Assert.True(await page.IsDisabledAsync("#tnwActiveFischer"));

            await page.FillAsync("#tnwActiveDenmarkSquadrons", "2");
            await page.PressAsync("#tnwActiveDenmarkSquadrons", "Tab");
            Assert.False(await page.IsDisabledAsync("#tnwActiveFischer"));

            // Four British (3 each) + two Danish (2 each) = 16 dice before Fischer.
            Assert.Contains("16", await page.TextContentAsync("#tnwActiveRound1Dice") ?? string.Empty);

            await page.CheckAsync("#tnwActiveFischer");

            // Fischer adds one die per Danish Squadron: 16 + 2 = 18.
            Assert.Contains("18", await page.TextContentAsync("#tnwActiveRound1Dice") ?? string.Empty);

            await page.FillAsync("#tnwActiveDenmarkSquadrons", "0");
            await page.PressAsync("#tnwActiveDenmarkSquadrons", "Tab");
            Assert.True(await page.IsDisabledAsync("#tnwActiveFischer"));
            Assert.False(await page.IsCheckedAsync("#tnwActiveFischer"));
        }

        [Fact]
        public async Task SmallBattle_RendersExactStats()
        {
            using IPlaywright playwright = await Playwright.CreateAsync();
            await using IBrowser browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            IPage page = await OpenPage(browser);
            await SetUpRulebookExample(page);

            await page.ClickAsync("#tnwNavalCalculateStats");
            await page.WaitForSelectorAsync("text=Exact Stats", new PageWaitForSelectorOptions { Timeout = 15000 });

            string bodyText = await page.TextContentAsync("#resultsSection") ?? string.Empty;
            Assert.Contains("Active Wins", bodyText);
        }

        [Fact]
        public async Task LargeBattle_FallsBackToMonteCarlo()
        {
            using IPlaywright playwright = await Playwright.CreateAsync();
            await using IBrowser browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            IPage page = await OpenPage(browser);
            await page.FillAsync("#tnwActiveBritainSquadrons", "10");
            await page.PressAsync("#tnwActiveBritainSquadrons", "Tab");
            await page.FillAsync("#tnwInactiveFranceSquadrons", "12");
            await page.PressAsync("#tnwInactiveFranceSquadrons", "Tab");

            await page.ClickAsync("#tnwNavalCalculateStats");
            await page.WaitForSelectorAsync("text=Monte Carlo Stats", new PageWaitForSelectorOptions { Timeout = 30000 });
        }

        [Fact]
        public async Task PortBattle_DisablesAndClearsEvasionDie()
        {
            using IPlaywright playwright = await Playwright.CreateAsync();
            await using IBrowser browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            IPage page = await OpenPage(browser);
            await SetUpRulebookExample(page);
            await page.CheckAsync("#tnwActiveEvasionDie");
            Assert.True(await page.IsCheckedAsync("#tnwActiveEvasionDie"));

            await page.SelectOptionAsync("#tnwNavalLocation", "EnemyFortressPort");

            Assert.True(await page.IsDisabledAsync("#tnwActiveEvasionDie"));
            Assert.False(await page.IsCheckedAsync("#tnwActiveEvasionDie"));
            // Two French (2 dice each) + two Spanish (1 each) + four Fortress shore battery dice.
            Assert.Contains("10", await page.TextContentAsync("#tnwInactiveRound1Dice") ?? string.Empty);
        }

        [Fact]
        public async Task RollOnce_RendersASingleBattleResult()
        {
            using IPlaywright playwright = await Playwright.CreateAsync();
            await using IBrowser browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            IPage page = await OpenPage(browser);
            await SetUpRulebookExample(page);

            await page.ClickAsync("#tnwNavalRollOnce");
            await page.WaitForSelectorAsync("text=Single Battle Result", new PageWaitForSelectorOptions { Timeout = 10000 });

            Assert.Contains("Round 1:", await page.TextContentAsync("#resultsSection") ?? string.Empty);
        }

        [Fact]
        public async Task EmptyActiveFleet_ShowsValidationError()
        {
            using IPlaywright playwright = await Playwright.CreateAsync();
            await using IBrowser browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            IPage page = await OpenPage(browser);
            await page.FillAsync("#tnwActiveBritainSquadrons", "0");
            await page.PressAsync("#tnwActiveBritainSquadrons", "Tab");

            await page.ClickAsync("#tnwNavalCalculateStats");

            await page.WaitForSelectorAsync("text=The Active Fleet needs at least one Squadron", new PageWaitForSelectorOptions { Timeout = 5000 });
        }
    }
}

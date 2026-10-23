from pathlib import Path
import re
from playwright.sync_api import sync_playwright, expect

output = Path("captures")
output.mkdir(exist_ok=True)

with sync_playwright() as playwright:
    browser = playwright.chromium.launch(headless=True, args=["--no-sandbox"])
    page = browser.new_page(viewport={"width": 1440, "height": 1080}, device_scale_factor=1)
    page.goto("http://127.0.0.1:8080", wait_until="domcontentloaded", timeout=60000)

    expect(page.locator(".estimate-card")).to_have_count(3, timeout=60000)
    expect(page.locator(".estimate-card").nth(1).locator(".estimate-value")).to_contain_text("10.85", timeout=30000)
    assert page.locator(".plot-wrap svg path").count() >= 3, "Published NTCP curves were not rendered."
    page.screenshot(path=str(output / "01-calculator-real-safe.png"), full_page=True, animations="disabled")

    page.locator("#lung-dose").fill("20.0")
    expect(page.locator(".estimate-card").nth(1).locator(".estimate-value")).to_contain_text("20.59", timeout=30000)
    page.screenshot(path=str(output / "02-calculator-recalculated-lung-20gy.png"), full_page=True, animations="disabled")

    page.locator(".reset-button").click()
    expect(page.locator(".estimate-card").nth(1).locator(".estimate-value")).to_contain_text("10.85", timeout=30000)
    page.get_by_role("button", name=re.compile("NTCP models")).click()
    expect(page.locator(".catalogue-item")).to_have_count(3)
    expect(page.locator(".model-detail h2")).to_contain_text("Cardiac mortality")
    page.screenshot(path=str(output / "03-ntcp-model-heart-real-safe.png"), full_page=True, animations="disabled")

    page.locator(".catalogue-item").nth(1).click()
    expect(page.locator(".model-detail h2")).to_contain_text("Radiation pneumonitis")
    page.screenshot(path=str(output / "04-ntcp-model-lung-real-safe.png"), full_page=True, animations="disabled")

    page.get_by_role("button", name="Calculator").click()
    expect(page.locator(".estimate-card")).to_have_count(3)
    page.get_by_role("button", name="Detail").click()
    expect(page.locator(".zoom-note")).to_be_visible()
    page.screenshot(path=str(output / "05-calculator-detail-real-safe.png"), full_page=True, animations="disabled")

    browser.close()

print("Captured real SAFE application views, including verified server-side recalculation: lungs 14 to 20 Gy.")
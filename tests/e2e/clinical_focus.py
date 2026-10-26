from pathlib import Path
from playwright.sync_api import sync_playwright, expect

BASE = "http://127.0.0.1:8080"
OUTPUT = Path("captures")
OUTPUT.mkdir(exist_ok=True)

with sync_playwright() as playwright:
    browser = playwright.chromium.launch(headless=True, args=["--no-sandbox"])
    page = browser.new_page(viewport={"width": 1440, "height": 960}, device_scale_factor=1)
    page.goto(BASE, wait_until="domcontentloaded", timeout=60000)

    expect(page.get_by_role("button", name="Clinical focus")).to_have_attribute("aria-pressed", "true", timeout=60000)
    expect(page.locator(".focus-estimate")).to_have_count(3, timeout=60000)
    expect(page.locator(".focus-chart .zoom-note")).to_contain_text("0–30 Gy", timeout=30000)
    expect(page.locator(".focus-chart .zoom-note")).to_contain_text("0–15%", timeout=30000)
    page.screenshot(path=str(OUTPUT / "01-clinical-focus-actual-safe.png"), full_page=True, animations="disabled")

    page.locator("#lung-dose").fill("30")
    expect(page.locator(".focus-estimate").nth(1)).to_contain_text("30.0 Gy", timeout=30000)
    expect(page.locator(".focus-chart .zoom-note")).to_contain_text("0–35 Gy", timeout=30000)
    expect(page.locator(".focus-chart .zoom-note")).to_contain_text("0–55%", timeout=30000)
    # The plotted dose/risk marker must remain within the inner scientific axes.
    bounds = page.locator(".focus-chart .plot-wrap svg circle[r='5']").evaluate_all(
        "(nodes) => nodes.map(node => ({x: Number(node.getAttribute('cx')), y: Number(node.getAttribute('cy'))}))"
    )
    assert len(bounds) == 3, f"Expected 3 model markers, found {bounds}"
    for marker in bounds:
        assert 76 < marker["x"] < 846, f"Marker x position clipped: {marker}"
        assert 40 < marker["y"] < 360, f"Marker y position clipped: {marker}"
    page.screenshot(path=str(OUTPUT / "02-focus-lung-30gy-adaptive-axes.png"), full_page=True, animations="disabled")

    page.get_by_role("button", name="Research detail").click()
    expect(page.locator(".input-pane")).to_be_visible()
    expect(page.locator("#lung-dose")).to_have_value("30")
    expect(page.locator(".estimate-card")).to_have_count(3)
    page.screenshot(path=str(OUTPUT / "03-preserved-research-detail.png"), full_page=True, animations="disabled")

    page.get_by_role("button", name="Clinical focus").click()
    expect(page.locator(".focus-estimate")).to_have_count(3)
    expect(page.locator("#lung-dose")).to_have_value("30")
    browser.close()

print("PASS: Focused SAFE view, live lung-dose recalculation, adaptive axes, marker visibility, and original research view preserved.")

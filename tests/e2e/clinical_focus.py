import re
from playwright.sync_api import sync_playwright, expect

BASE = "http://127.0.0.1:8080"

with sync_playwright() as playwright:
    browser = playwright.chromium.launch(headless=True, args=["--no-sandbox"])
    page = browser.new_page(viewport={"width": 1440, "height": 960}, device_scale_factor=1)
    page.goto(BASE, wait_until="domcontentloaded", timeout=60000)

    expect(page.locator(".focus-estimate")).to_have_count(3, timeout=60000)
    expect(page.locator(".nav-count")).to_have_text("29", timeout=30000)
    expect(page.locator(".lean-metric-row")).to_have_count(3)
    for name in ("Heart · Dmean", "Lungs · Dmean", "Liver · Dmean"):
        assert page.get_by_text(name, exact=True).count() == 1, f"Missing required DVH metric label: {name}"

    for removed in (".page-heading", ".example-banner", ".display-mode-switch", ".prototype-label", ".focus-advanced-link"):
        assert page.locator(removed).count() == 0, f"Unwanted explanatory or display-mode content: {removed}"

    nav = page.locator(".main-nav").bounding_box()
    brand = page.locator(".brand").bounding_box()
    assert nav["x"] > brand["x"] + brand["width"] + 200, "Main navigation is not aligned to the right"

    graph = page.locator(".focus-chart").bounding_box()
    results = page.locator(".focus-summary").bounding_box()
    assert graph["y"] < results["y"], "Scientific plot must appear above estimates"

    expect(page.locator(".focus-estimate-head")).to_contain_text("ΔNTCP (−1 Gy)")
    assert page.locator(".focus-estimate-delta").count() == 3, "Each endpoint needs one-Gy NTCP reduction"
    assert page.locator(".focus-legend .legend-delta").count() == 3, "Graph legend must expose the same sensitivity"

    page.locator("#lung-dose").fill("30")
    expect(page.locator(".focus-estimate").nth(1)).to_contain_text("30.0 Gy", timeout=30000)
    assert page.locator(".focus-estimate").nth(1).locator(".focus-estimate-delta").inner_text().endswith("pp")
    expect(page.locator(".plot-wrap .zoom-note")).to_contain_text("0–35 Gy", timeout=30000)
    expect(page.locator(".plot-wrap .zoom-note")).to_contain_text("0–55%", timeout=30000)

    markers = page.locator(".focus-chart .plot-wrap svg circle[r='5']").evaluate_all(
        "(nodes) => nodes.map(n => ({x:Number(n.getAttribute('cx')), y:Number(n.getAttribute('cy'))}))"
    )
    assert len(markers) == 3, f"Expected all 3 modeled doses: {markers}"
    assert all(76 < m["x"] < 846 and 40 < m["y"] < 360 for m in markers), "An NTCP marker is clipped"
    assert page.locator(".focus-chart .plot-wrap svg circle[r='3.4']").count() == 3, "One-Gy lower marker absent"

    lung = page.locator(".focus-legend button").filter(has_text="Lungs")
    lung.click()
    expect(lung).to_have_attribute("aria-pressed", "true")
    lung.click()
    expect(lung).to_have_attribute("aria-pressed", "false")

    page.get_by_role("button", name=re.compile("NTCP models")).click()
    expect(page.locator(".catalogue-item")).to_have_count(29)
    page.get_by_role("button", name="Calculator", exact=True).click()
    expect(page.locator("#lung-dose")).to_have_value("30")

    browser.close()

print("PASS: single lean SAFE view, right navigation, precise Dmean labels, plot first, one-Gy NTCP reduction, dynamic axes, and preserved catalogue.")

from pathlib import Path
from playwright.sync_api import sync_playwright, expect

BASE = "http://127.0.0.1:8080"
Path("captures").mkdir(exist_ok=True)

with sync_playwright() as playwright:
    browser = playwright.chromium.launch(headless=True, args=["--no-sandbox"])
    page = browser.new_page(viewport={"width": 1440, "height": 1100})
    page.goto(BASE, wait_until="domcontentloaded", timeout=60000)
    expect(page.locator(".estimate-card")).to_have_count(3, timeout=60000)
    expect(page.locator(".nav-count")).to_have_text("29", timeout=30000)
    page.get_by_role("button", name="NTCP models").click()
    expect(page.locator(".catalogue-item")).to_have_count(29, timeout=30000)

    page.locator(".catalogue-item").filter(has_text="Valvular disease").click()
    expect(page.locator(".runner-field input")).to_have_count(1)
    page.locator(".runner-field input").fill("40.1")
    page.get_by_role("button", name="Calculate selected model").click()
    expect(page.locator(".runner-value")).to_contain_text("50.000", timeout=30000)
    expect(page.locator(".model-runner-plot")).to_be_visible()
    page.screenshot(path="captures/01-heart-valvular-gamma50-safe.png", full_page=True)

    page.locator(".catalogue-item").filter(has_text="Laryngeal edema").click()
    expect(page.locator(".runner-field")).to_contain_text("EQD2 Gy")
    page.locator(".runner-field textarea").fill("47.3:0.3,47.3:0.7")
    page.get_by_role("button", name="Calculate selected model").click()
    expect(page.locator(".runner-value")).to_contain_text("50.000", timeout=30000)
    page.screenshot(path="captures/02-larynx-full-dvh-lkb-safe.png", full_page=True)

    page.locator(".catalogue-item").filter(has_text="Lung cancer").click()
    page.locator(".runner-field input").fill("10")
    page.get_by_role("button", name="Calculate selected model").click()
    expect(page.locator(".runner-value")).to_contain_text("RR 2.4100", timeout=30000)
    expect(page.locator(".model-runner-plot")).to_have_count(0)
    page.screenshot(path="captures/03-lung-relative-risk-safe.png", full_page=True)

    page.locator(".catalogue-item").filter(has_text="Spleen toxicity").click()
    expect(page.locator(".model-runner")).to_have_count(0)
    page.screenshot(path="captures/04-nonexecutable-evidence-safe.png", full_page=True)

    browser.close()

print("PASS: 29 entries; gamma50; typed EQD2 DVH LKB; RR (separate scale); blocked models; real SAFE server.")

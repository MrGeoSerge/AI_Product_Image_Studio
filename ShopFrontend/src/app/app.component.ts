import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet],
  template: `
    <div class="container">
      <header class="app-header">
        <div class="app-header-main">
          <div class="app-logo-circle">AI</div>
          <div>
            <h1>AI Product Image Studio</h1>
            <p>Create products and instantly generate on-body previews with different skin tones.</p>
          </div>
        </div>
        <div class="app-header-badge">
          <span>ComfyUI powered</span>
        </div>
      </header>
      <router-outlet></router-outlet>
    </div>
  `,
  styles: [`
    .app-header {
      margin-bottom: 30px;
      padding: 18px 22px;
      border-radius: 14px;
      background: radial-gradient(circle at top left, #e0f2fe, #eff6ff 40%, #f9fafb);
      border: 1px solid #d0e3ff;
      display: flex;
      justify-content: space-between;
      align-items: center;
      gap: 16px;
    }

    .app-header-main {
      display: flex;
      align-items: center;
      gap: 16px;
    }

    .app-logo-circle {
      width: 42px;
      height: 42px;
      border-radius: 999px;
      background: linear-gradient(135deg, #2563eb, #4f46e5);
      display: flex;
      align-items: center;
      justify-content: center;
      color: #fff;
      font-weight: 700;
      letter-spacing: 0.03em;
      box-shadow: 0 10px 25px rgba(37, 99, 235, 0.35);
    }

    header h1 {
      font-size: 24px;
      color: #0f172a;
      margin-bottom: 2px;
    }

    header p {
      color: #64748b;
      margin-top: 0;
      font-size: 14px;
    }

    .app-header-badge span {
      display: inline-flex;
      align-items: center;
      gap: 6px;
      padding: 6px 12px;
      border-radius: 999px;
      font-size: 12px;
      font-weight: 500;
      background: rgba(15, 23, 42, 0.03);
      color: #334155;
      border: 1px solid rgba(148, 163, 184, 0.5);
    }
  `]
})
export class AppComponent {
  title = 'shop-frontend';
}


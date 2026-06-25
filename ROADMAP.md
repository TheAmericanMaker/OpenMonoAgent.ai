# Roadmap

<div align="center">
  <table width="760" style="border-collapse:collapse;background:#111111;border:1px solid #232323;border-radius:4px;">
    <tr>
      <td colspan="3" style="padding:8px 16px 6px;border-bottom:1px solid #232323;">
        <code style="font-size:11px;color:#A3FF66;letter-spacing:0.12em;">● OPENMONO ROADMAP · 2026</code>
      </td>
    </tr>
    <tr>
      <!-- IN PROGRESS -->
      <td width="33%" valign="top" style="padding:14px 16px;border-right:1px solid #232323;border-top:2px solid #A3FF66;">
        <code style="font-size:10px;color:#A3FF66;letter-spacing:0.1em;">IN PROGRESS</code><br/><br/>
        <sub style="color:#6A6A62;line-height:2;">
          ⬡ VS Code extension — v2 improvements &amp; polish<br/>
          ⬡ Colored diff display for file edits<br/>
          ⬡ Git branch + status in system prompt<br/>
          ⬡ Built-in playbooks — commit, review, explain, debug<br/>
          ⬡ <code>/doctor</code> — diagnostics &amp; connectivity check<br/>
          ⬡ Antigravity store extension<br/>
        </sub>
      </td>
      <!-- COMING NEXT -->
      <td width="33%" valign="top" style="padding:14px 16px;border-right:1px solid #232323;border-top:2px solid #4A7A28;">
        <code style="font-size:10px;color:#6AAA38;letter-spacing:0.1em;">COMING NEXT</code><br/><br/>
        <sub style="color:#6A6A62;line-height:2;">
          ⬡ Parallel sub-agent coordination<br/>
          ⬡ Playwright MCP — browser automation<br/>
          ⬡ MCP manager — discover &amp; configure servers in-agent<br/>
          ⬡ Thinking budget token cap<br/>
          ⬡ <code>/commit</code> and <code>/review</code> slash commands<br/>
          ⬡ Background agent system + output streaming<br/>
          ⬡ Micro-compaction before full compaction<br/>
          ⬡ Session revert — roll back to any prior turn<br/>
        </sub>
      </td>
      <!-- ON THE HORIZON -->
      <td width="33%" valign="top" style="padding:14px 16px;border-top:2px solid #2A2A2A;">
        <code style="font-size:10px;color:#4A4A44;letter-spacing:0.1em;">ON THE HORIZON</code><br/><br/>
        <sub style="color:#6A6A62;line-height:2;">
          ⬡ Desktop app — Tauri wrapper<br/>
          ⬡ Web frontend for remote access<br/>
          ⬡ Theme system — light / dark / custom<br/>
          ⬡ Vim keybinding mode<br/>
          ⬡ More providers — Vertex, Azure, Bedrock, Groq<br/>
          ⬡ Slack + GitHub App integrations<br/>
          ⬡ Session tagging &amp; forking<br/>
          ⬡ File watching during active sessions<br/>
          ⬡ ACP support — Zed and other editors<br/>
          ⬡ Auto-dreaming — background memory consolidation<br/>
          ⬡ Opt-in OpenTelemetry tracing<br/>
          ⬡ Multi-repo workspaces<br/>
          ⬡ Voice input mode<br/>
          ⬡ Agent marketplace — share &amp; import playbooks<br/>
        </sub>
      </td>
    </tr>
  </table>
</div>

---

## Shipped

<div align="center">
  <table width="760" style="border-collapse:collapse;background:#111111;border:1px solid #232323;border-radius:4px;">
    <tr>
      <td colspan="2" style="padding:8px 16px 6px;border-bottom:1px solid #232323;">
        <code style="font-size:11px;color:#A3FF66;letter-spacing:0.12em;">✓ DONE</code>
      </td>
    </tr>
    <tr>
      <td width="50%" valign="top" style="padding:12px 16px;border-right:1px solid #232323;">
        <sub style="color:#4A4A44;line-height:2.2;">
          ✓ <span style="color:#6A6A62;">Web search &amp; scraping — SearXNG + Scrapling, self-hosted, gateway auto-detected</span><br/>
          ✓ <span style="color:#6A6A62;">Vision / multimodal — <code>@image.png</code> in chat, mmproj auto-downloaded, smart VRAM resize</span><br/>
          ✓ <span style="color:#6A6A62;">Mobile app — iOS &amp; Android + <a href="https://apps.apple.com/us/app/openmono-ai-coding-agent/id6766077801">App Store</a> / <a href="https://play.google.com/store/apps/details?id=ai.openmonoagent.app">Google Play</a></span><br/>
          ✓ <span style="color:#6A6A62;">Plan mode — restricts agent to read-only tools before making changes</span><br/>
          ✓ <span style="color:#6A6A62;">Session resume — <code>/resume [id]</code> restores any prior session from JSONL</span><br/>
        </sub>
      </td>
      <td width="50%" valign="top" style="padding:12px 16px;">
        <sub style="color:#4A4A44;line-height:2.2;">
          ✓ <span style="color:#6A6A62;">VS Code + Cursor extension — chat panel via ACP server</span><br/>
          ✓ <span style="color:#6A6A62;"><code>openmono stop</code> — llama-server shutdown fixed</span><br/>
          ✓ <span style="color:#6A6A62;">Doom loop detection — aborts if same tool sequence repeats 3×</span><br/>
          ✓ <span style="color:#6A6A62;">Agent iteration limit raised to 1000, now configurable</span><br/>
          ✓ <span style="color:#6A6A62;">12 GB + 16 GB GPU support — installer auto-selects model by VRAM tier</span><br/>
          ✓ <span style="color:#6A6A62;">Distributed inference — agent on laptop, model on GPU box, one relay port</span><br/>
        </sub>
      </td>
    </tr>
  </table>
</div>

---

*[Contributing](CONTRIBUTING.md) · [Architecture](docs/ARCHITECTURE.md)*

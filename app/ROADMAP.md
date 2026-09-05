# DrumDuino UI Roadmap

## P0 — Implementado

- StatusBar unificada (modo LED, COM, MIDI IN, kit, badges RAM/EEPROM, undo)
- Tabs **Config | Monitor | Treino** com ícones Path (sem Unicode □)
- Lista de pads custom (busca, multi-select, badge diff, mini velocity)
- Editor agrupado (Identidade, Sensibilidade, Curva, Crosstalk)
- Monitor dock na tela Config
- **Peak-hold**: barra de velocity fica no último hit (não decai)
- Tela Analytics: monitor, timeline, heatmap, intensidade
- Badges divergência RAM/EEPROM por pad e na barra

## P1 — Implementado

- Profiles por pad (salvar/carregar em `%AppData%/DrumDuino/profiles`)
- Curva: presets + CurveForm + preview SVG
- Single-pad mode no monitor Analytics
- Detector de crosstalk (overlay na timeline + mensagem)
- Assistente de threshold (wizard 10 golpes)
- Test note via MIDI OUT
- Multi-select + aplicar threshold/curva em lote
- Duplicar pad (origem → índice destino)
- Reordenar pads (Up/Dn troca configs entre índices)
- Undo/redo com snapshots locais
- Reset → EEPROM baseline

## Treino de dinâmica (Beat Studio-style)

- Aba **Treino**: gráfico força × tempo com envelope-alvo
- Padrões: Crescendo, Decrescendo, Swell, Steady
- Faixa de tolerância + score % em tempo real
- Opção focar só no pad selecionado
- Referência: Beat Studio Live Play Dynamics Graph

## P2 — Pendente

- Editor gráfico de curva custom (placeholder na UI)
- Auto-detect COM (VID/PID / handshake)
- Log serial SysEx (aba Debug)
- Temas: Dark / OLED / alto contraste
- Tooltips educativos com link para docs
- Drag-and-drop nativo na lista de pads
- Metrônome no treino de dinâmica
- Histórico de scores de treino

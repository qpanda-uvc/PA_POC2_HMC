# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Unity Factory Automation Agent — an AI-powered system that uses LLMs and Model Context Protocol (MCP) to plan and execute multi-step factory automation tasks in Unity. Users describe what they want built (e.g., "place 6 pallets in a 2x3 grid"), the Planner generates a structured step-by-step plan, and the Executor maps each step to MCP tool calls against a running Unity editor.

## Running

```bash
python main.py
```

**Prerequisites:**
- Python 3.13+ with `langchain_core`, `langchain_ollama`, `langchain_openai`, `langchain_google_genai`, `mcp`, `python-dotenv`
- Ollama running locally with `qwen2.5:14b` model pulled
- Unity editor open with the MCP Unity server package installed
- `key.env` in project root with `GOOGLE_API_KEY` and `Zhipu_API_KEY`

No formal test suite, build step, or linter is configured. The app is interactive (terminal input/output) and requires a live Unity MCP server connection.

## Architecture

All code lives in `main.py` (~600 lines). The other `main copy*.py` and `old_main*.py` files are historical snapshots — `main.py` is the current version.

### State Machine

`AgentState` (TypedDict) carries all workflow state: chat messages, the structured plan (list of step dicts), current step index, execution results, MCP tool schemas, Unity scene state, and available prefab paths.

### Two-LLM Design

- **Planner** (`planner_node`) — uses GLM-4.7-flash via Zhipu API (OpenAI-compatible endpoint). Generates a JSON array of steps with natural-language descriptions, prefab hints, and pre-calculated position arrays. Handles replanning from any step when the user provides feedback.
- **Executor** (`executor_node`) — uses qwen2.5:14b via local Ollama. Takes one step at a time, maps it to concrete MCP tool calls (tool name + params JSON), and executes them sequentially against Unity. Resolves dynamic parameters (asset paths, GUIDs) from previous step results.

### Key Helpers

- `get_available_tools(session)` — queries MCP for tool schemas (name, params, required markers)
- `get_current_scene_state(session)` — fetches Unity scene hierarchy via `get_scene_info` or `get_hierarchy`
- `search_prefabs_for_plan(session, plan)` — extracts keywords from the plan via LLM, then calls `search_prefabs` MCP tool for each keyword to get real asset paths/GUIDs
- `resolve_params_with_llm(llm, params, context)` — resolves `"이전 단계 결과에서 추출"` placeholder values; uses regex for `assetPath`/`guid`, LLM for other params
- `repair_truncated_json(text)` — attempts to fix truncated JSON from LLM output by closing brackets/braces

### Workflow (`run_workflow`)

1. Connect to Unity MCP server via stdio
2. Query tools, scene state, and prefabs
3. **Plan phase loop**: user request → Planner generates plan → user confirms, provides feedback (triggers replan), or cancels
4. **Execution phase loop**: for each step → display → user chooses to execute, replan from this step, or stop → Executor runs MCP tool calls (3 retries each)
5. Summary with error count

## Conventions

- Comments and prompts are in Korean (한국어)
- LLM prompts are embedded directly in node functions as f-strings
- JSON parsing from LLM output always strips markdown fences, comments, and trailing commas before extraction
- The Planner explicitly forbids mentioning tool names — steps describe intent only; the Executor handles tool mapping
- Positions are pre-calculated arrays in the plan to prevent LLM coordinate hallucination

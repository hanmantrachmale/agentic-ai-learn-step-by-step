# POC 3 Build Guide — RAG over Your Own Logs (Semantic Kernel + Ollama)
### Build-it-yourself roadmap — no copy-paste code, just steps and concepts

**Goal of this POC:** the AI answers questions using YOUR data (e.g. past
incident logs) instead of guessing from its training data. You'll ask
"What's the likely cause of this new error?" and the bot will find
similar past incidents from your own records and use them to ground its
answer — this is the "RAG" pattern (Retrieval-Augmented Generation).

This builds directly on POC2's Kernel/plugin concepts, but introduces a
new capability: **embeddings** and **vector search**.

---

## 0. What problem does RAG actually solve? (read before building)

An LLM only knows what it was trained on — it has never seen your
company's specific incident history, your YOOBIC integration quirks, or
last month's Application Insights exceptions. RAG works around this
without retraining the model, by:
1. Converting your existing text records (e.g. past incidents) into
   **embeddings** — numeric vectors that capture semantic meaning.
2. Storing those vectors in a **vector store**.
3. When a new question/error comes in, converting IT into an embedding
   too, and finding the most semantically similar stored vectors
   (**similarity search**).
4. Injecting the retrieved text into the prompt as context, so the model
   answers using real, relevant information instead of fabricating one <cite>turn11search4</cite><cite>turn11search5</cite>.

**Key insight:** "similar meaning" is not the same as "similar words."
Embeddings capture meaning, so "login fails intermittently" and
"authentication times out randomly" can be found as similar even though
they share almost no words in common.

---

## ⚠️ Ollama-specific setup notes — read before starting

**You need TWO different local models for this POC:**
- A **chat model** (you likely already have this from POC2, e.g.
  `llama3.1:8b`) — generates the final natural-language answer.
- An **embedding model** — a different, smaller model whose only job is
  turning text into vectors. Pull one now:
  ```bash
  ollama pull nomic-embed-text
  ```
  This is a well-regarded, lightweight embedding model that runs
  comfortably on CPU <cite>turn11search12</cite><cite>turn11search17</cite>.

**Good news on reliability:** unlike function-calling (POC2's known
Ollama gotcha), Semantic Kernel's dedicated Ollama connector has
supported **text embedding generation** for a long time and is
straightforward to use — this part doesn't need the OpenAI-compatible
endpoint workaround <cite>turn11search21</cite><cite>turn11search23</cite>.

---

## Milestone 0 — Prerequisites
- [ ] Ollama running, with both a chat model AND `nomic-embed-text` pulled.
- [ ] Your POC2 project (or a fresh copy) as a starting point — you'll
      reuse the Kernel-building pattern from there.

**Do this:**
1. Confirm both models are present: `ollama list` should show your chat
   model and `nomic-embed-text`.
2. Optional sanity check before writing any C#: test the embedding
   endpoint directly:
   ```bash
   curl http://localhost:11434/api/embed -d '{"model": "nomic-embed-text", "input": "test sentence"}'
   ```
   You should get back a JSON object containing a numeric array — that
   array IS the embedding <cite>turn11search13</cite><cite>turn11search15</cite>.
3. In your project, install two additional packages:
   - `Microsoft.SemanticKernel.Connectors.Ollama` (adds embedding support
     for Ollama — note this is a prerelease/alpha package, so you'll need
     `--prerelease`)
   - `CommunityToolkit.VectorData.InMemory` (a simple, no-external-database
     vector store — perfect for a POC before touching a real database) <cite>turn11search1</cite>

**Checkpoint:** `dotnet build` succeeds with both packages restored.

---

## Milestone 1 — Generate your first embedding (prove the mechanism works)
**Concept to understand first:** an embedding generation service is
conceptually similar to a chat service — you get an interface from the
kernel/builder, then call one method to convert text into numbers.

**What to do:**
1. In a throwaway test file or temporary code in `Program.cs`, build an
   embedding generation service pointed at your local Ollama instance and
   the `nomic-embed-text` model (hint: search for how the Ollama
   connector exposes an `OllamaApiClient` and a method like
   `AsTextEmbeddingGenerationService` — or look up
   `AddOllamaTextEmbeddingGeneration` as a kernel builder extension) <cite>turn11search14</cite><cite>turn11search19</cite>.
2. Call the method that generates an embedding for a single string,
   e.g. `"The store's POS terminal is offline"`.
3. Print out the length of the resulting vector (should be a few hundred
   numbers) and the first 5 values, just to prove it's real, non-trivial
   output.

**Checkpoint:** running this prints a vector of consistent length every
time you embed a sentence — this proves your embedding pipeline works,
completely independent of chat/RAG logic.

**Common mistake:** trying to use your CHAT model (e.g. `llama3.1:8b`) to
generate embeddings. Chat models and embedding models are different
model types — always use `nomic-embed-text` (or another dedicated
embedding model) for this step.

---

## Milestone 2 — Design your mock knowledge base
**Concept to understand first:** before wiring up a real vector store,
design the data you'll search over. Pick something realistic to your
actual work — e.g. a handful of **past incident summaries** (the kind of
thing you'd find in Application Insights or a support ticket log).

**What to do:**
1. Create a small in-memory list of 5–8 mock "incident records," each
   with at minimum: an ID, a short text description, and maybe a
   resolution/root-cause note. Example themes relevant to your work:
   YOOBIC auth token expiry issues, POS terminal offline errors, franchisee
   file import failures, etc. (Keep this fictional/mocked for now — real
   data comes in the stretch milestone.)
2. Keep this as a plain C# list/array for now — no vector store yet.

**Checkpoint:** you have a hardcoded list of realistic-sounding incident
records you can reference by eye, so later you can sanity-check whether
retrieval results "make sense" to a human.

---

## Milestone 3 — Define a vector store record type
**Concept to understand first:** Semantic Kernel's vector store
connectors expect you to define a C# class/record describing your data
shape, with special attributes marking which property is the key, which
are plain data, and which holds the vector itself <cite>turn11search2</cite>.

**What to do:**
1. Create a new record/class representing one "chunk" in your vector
   store — it needs at least: a string ID (the key), the original text
   (a data field), and a vector field (commonly typed as
   `ReadOnlyMemory<float>`).
2. Look up the specific attributes used to annotate these fields for
   Semantic Kernel's vector store abstraction (hint: search "Vector Store
   Record attributes Semantic Kernel" — you're looking for something like
   a Key attribute, a Data attribute, and a Vector attribute).

**Checkpoint:** the type compiles, with attributes correctly applied —
no runtime logic yet, just the shape of your data.

---

## Milestone 4 — Set up the in-memory vector store and ingest your data
**Concept to understand first:** "ingesting" means: for each mock
incident record, generate its embedding, then save (upsert) it into the
vector store as an instance of the record type from Milestone 3.

**What to do:**
1. Register the in-memory vector store with your kernel builder (hint:
   look for a service-collection extension method, something like
   `AddInMemoryVectorStore()`) <cite>turn11search1</cite>.
2. Get a "collection" object from the vector store (think of it like a
   named table) for a chosen collection name, e.g. `"incidents"`.
3. Loop over your Milestone 2 mock data. For each record:
   - Generate its embedding using the service from Milestone 1.
   - Construct an instance of your Milestone 3 record type, filling in
     the ID, original text, and the embedding vector.
   - Upsert it into the collection (hint: look for an `UpsertAsync`-style
     method).

**Checkpoint:** after ingestion, write a temporary check that reads back
the count of items in the collection (or just trust the upsert calls
succeeded without exceptions) — confirm no exceptions were thrown during
ingestion of all mock records.

---

## Milestone 5 — Perform your first similarity search
**Concept to understand first:** to search, you embed the user's
QUESTION using the exact same embedding model, then ask the vector store
for the top-K most similar stored vectors, typically using **cosine
similarity** as the distance metric <cite>turn11search15</cite><cite>turn11search1</cite>.

**What to do:**
1. Pick a test question that should semantically match one of your mock
   incidents, but doesn't share many exact words (e.g. if you have an
   incident about "YOOBIC token expiring after 20 minutes," ask something
   like "why does my API session keep needing to reauthenticate?").
2. Generate an embedding for that question using the same embedding
   service from Milestone 1.
3. Call the vector store collection's search method (hint: look for
   something like `SearchAsync` or `VectorizedSearchAsync`), passing in
   the query embedding and a "top K" value (e.g. top 2–3 results).
4. Print out the retrieved records' text and their similarity scores.

**Checkpoint:** the top result should be the incident record you expect
a human to consider "most relevant," even without exact keyword overlap.
If results seem random, double check you're using the SAME embedding
model for both ingestion and querying — this is the single most common
RAG bug <cite>turn11search15</cite>.

---

## Milestone 6 — Build the full RAG prompt
**Concept to understand first:** now you combine retrieval with
generation. Instead of sending the user's raw question straight to the
chat model, you build an augmented prompt that includes the retrieved
context, then ask the chat model to answer using ONLY that context.

**What to do:**
1. After retrieving the top-K relevant records (Milestone 5), format them
   into a text block, e.g. "Similar past incidents: 1) ... 2) ...".
2. Construct a system or user message that explicitly instructs the model
   to use this retrieved context to answer, and to say so if the context
   doesn't seem to cover the question (this reduces hallucination risk,
   though doesn't eliminate it entirely — RAG improves grounding, it
   doesn't guarantee perfect accuracy) <cite>turn11search4</cite>.
3. Send this augmented prompt through your existing chat completion
   service (same pattern as POC1/POC2) and print the final answer.

**Checkpoint:** ask a new, realistic question and confirm the model's
answer references specifics from your mock incident data (e.g. mentions
a root cause or resolution that only exists in your mock records) —
proving it actually used retrieved context, not generic knowledge.

---

## Milestone 7 — Wrap it in a chat loop
**What to do:**
1. Combine Milestones 5 and 6 into a single function: given a user
   question, embed it, retrieve top-K context, build the augmented
   prompt, call the chat model, return the answer.
2. Put this inside the same `while(true)` console loop pattern from
   POC1/POC2, so you get an interactive RAG chatbot.

**Checkpoint — full test pass:**

| Ask | Expected |
|---|---|
| A question closely matching one mock incident | Answer references that incident's specific details |
| A question with different words but same meaning as a mock incident | Still retrieves correctly (proves semantic, not keyword, matching) |
| A question totally unrelated to any mock incident | Model should indicate it doesn't have relevant information, rather than confidently fabricating an answer |
| Two questions that could match two different incidents | Confirm top-K retrieval brings back both when K≥2 |

---

## Milestone 8 (stretch) — Make it real
Once the above works end-to-end with mock data:
1. Replace your Milestone 2 mock list with a real export of Application
   Insights exceptions or past support tickets (a CSV or JSON export is
   fine to start).
2. You'll likely need very basic **chunking** if any single record is
   long (e.g. splitting a long log entry into smaller pieces) — look up
   "text chunking" in Semantic Kernel's docs if you hit this.
3. Swap the in-memory vector store for a persistent one only if you need
   data to survive app restarts (Qdrant, Azure AI Search, and others are
   supported via similar connector patterns) — for a POC, in-memory is
   completely fine and keeps things simple <cite>turn11search1</cite><cite>turn11search5</cite>.
4. Consider (advanced, optional) combining this with POC2's function
   calling: let the model choose between calling a YOOBIC lookup function
   OR searching incident history, depending on the question.

---

## Concepts glossary (for quick reference while building)
- **Embedding** — a numeric vector representing the semantic meaning of
  text; similar meanings produce similar (nearby) vectors.
- **Embedding model** — a specialized model (e.g. `nomic-embed-text`)
  whose only job is producing embeddings; different from chat models.
- **Vector store** — a database (or in-memory structure) optimized for
  storing vectors and finding the most similar ones quickly.
- **Cosine similarity** — the standard math measure of "how similar are
  two vectors," used to rank search results.
- **Top-K retrieval** — fetching the K most similar records to a query.
- **Grounding** — anchoring the model's answer to retrieved real data
  instead of letting it generate purely from training knowledge.
- **RAG (Retrieval-Augmented Generation)** — the overall pattern:
  retrieve relevant context, then generate an answer using that context.

---

## If you get stuck
Tell me:
1. Which milestone you're on.
2. What you tried.
3. The exact error, or — for retrieval-quality issues — what question you
   asked and what came back (retrieval bugs are often "worked but wrong
   result" rather than exceptions, so describe the *behavior*, not just
   errors).

Same approach as before — I'll help you debug the specific issue rather
than handing you a fix outright.
-------------------------------------------------------------------------------------------------

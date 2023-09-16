# ChatGPT Prompt Engineering for Developers

## Introduction

The course is about prompting with LLMs and will cover best practices for software development, common use cases, and building a chatbot using an LLM.
    
There are two types of LLMs: base LLMs and instruction-tuned LLMs. Instruction-tuned LLMs are recommended for most practical applications today because they are easier to use and safer.
    
Instruction-tuned LLMs are trained to follow instructions, which makes them helpful, honest, and harmless.
    
When using an instruction-tuned LLM, it is important to be clear and specific with instructions and to give the LLM time to think.

Instruction for a LLM:

- Be specific and clear. For example, if you want text about Alan Turing, specify whether you want the text to focus on his scientific work, personal life, or role in history.
- Specify what tone the text should take.
- If possible, provide the LLM with snippets of text to read in advance to help it generate the desired text.

## Principals

1. Write clear and specififc instructions.
2. Give the model time to think.

### Write clear and specififc instructions

Clear != short

#### Tactic 1: Use delimiters

Triple quotes (`` """ ``)
Triple backticks (`` ``` ``)
Triple dashes (`` --- ``)
Angle brackets (`` < > ``)
XML tags (`` <tag></tag> ``)

Example:

```python
text = f"""
You should express what you want a model to do by \ 
providing instructions that are as clear and \ 
specific as you can possibly make them. \ 
This will guide the model towards the desired output, \ 
and reduce the chances of receiving irrelevant \ 
or incorrect responses. Don't confuse writing a \ 
clear prompt with writing a short prompt. \ 
In many cases, longer prompts provide more clarity \ 
and context for the model, which can lead to \ 
more detailed and relevant outputs.
"""
prompt = f"""
Summarize the text delimited by triple backticks \ 
into a single sentence.
---{text}---
"""
response = get_completion(prompt)
print(response)
```

- These delimiters give the model a clear hint, that it should use the text inside.
- Using delimiters is also a helpful technique to try and avoid prompt injections.

#### Tactic 2: Ask for a structured output

```python
prompt = f"""
Generate a list of three made-up book titles along \ 
with their authors and genres. 
Provide them in JSON format with the following keys: 
book_id, title, author, genre.
"""
response = get_completion(prompt)
print(response)
```

#### Tactic 3: Ask the model to check whether conditions are satisfied

Check assumptions required to do the task.

```python
text_1 = f"""
Making a cup of tea is easy! First, you need to get some \ 
water boiling. While that's happening, \ 
grab a cup and put a tea bag in it. Once the water is \ 
hot enough, just pour it over the tea bag. \ 
Let it sit for a bit so the tea can steep. After a \ 
few minutes, take out the tea bag. If you \ 
like, you can add some sugar or milk to taste. \ 
And that's it! You've got yourself a delicious \ 
cup of tea to enjoy.
"""
prompt = f"""
You will be provided with text delimited by triple quotes. 
If it contains a sequence of instructions, \ 
re-write those instructions in the following format:

Step 1 - ...
Step 2 - …
…
Step N - …

If the text does not contain a sequence of instructions, \ 
then simply write \"No steps provided.\"

\"\"\"{text_1}\"\"\"
"""
response = get_completion(prompt)
print("Completion for Text 1:")
print(response)
```

#### Tactic 4: "Few-shot" prompting

Give successful examples of completing tasks. Then ask model to perform th task.

```python
prompt = f"""
Your task is to answer in a consistent style.

<child>: Teach me about patience.

<grandparent>: The river that carves the deepest \ 
valley flows from a modest spring; the \ 
grandest symphony originates from a single note; \ 
the most intricate tapestry begins with a solitary thread.

<child>: Teach me about resilience.
"""
response = get_completion(prompt)
print(response)
```


### Give the model time to think

#### Tactic 1: Specify the steps required to complete a task

Step 1: ...
Step 2: ...
...
Step N: ...

```python
text = f"""
In a charming village, siblings Jack and Jill set out on \ 
a quest to fetch water from a hilltop \ 
well. As they climbed, singing joyfully, misfortune \ 
struck—Jack tripped on a stone and tumbled \ 
down the hill, with Jill following suit. \ 
Though slightly battered, the pair returned home to \ 
comforting embraces. Despite the mishap, \ 
their adventurous spirits remained undimmed, and they \ 
continued exploring with delight.
"""
# example 1
prompt_1 = f"""
Perform the following actions: 
1 - Summarize the following text delimited by triple \
backticks with 1 sentence.
2 - Translate the summary into French.
3 - List each name in the French summary.
4 - Output a json object that contains the following \
keys: french_summary, num_names.

Separate your answers with line breaks.

Text:
---{text}---
"""
response = get_completion(prompt_1)
print("Completion for prompt 1:")
print(response)
```

#### Tactic 2: Instruct the model to work out its own solution before rushing to a conclusion


```python
prompt = f"""
Determine if the student's solution is correct or not.

Question:
I'm building a solar power installation and I need \
 help working out the financials. 
- Land costs $100 / square foot
- I can buy solar panels for $250 / square foot
- I negotiated a contract for maintenance that will cost \ 
me a flat $100k per year, and an additional $10 / square \
foot
What is the total cost for the first year of operations 
as a function of the number of square feet.

Student's Solution:
Let x be the size of the installation in square feet.
Costs:
1. Land cost: 100x
2. Solar panel cost: 250x
3. Maintenance cost: 100,000 + 100x
Total cost: 100x + 250x + 100,000 + 100x = 450x + 100,000
"""
response = get_completion(prompt)
print(response)
```

Note that the student's solution is actually not correct.
We can fix this by instructing the model to work out its own solution first.

```python
prompt = f"""
Your task is to determine if the student's solution \
is correct or not.
To solve the problem do the following:
- First, work out your own solution to the problem. 
- Then compare your solution to the student's solution \ 
and evaluate if the student's solution is correct or not. 
Don't decide if the student's solution is correct until 
you have done the problem yourself.

Use the following format:
Question:
---
question here
---
Student's solution:
---
student's solution here
---
Actual solution:
---
steps to work out the solution and your solution here
---
Is the student's solution the same as actual solution \
just calculated:
---
yes or no
---
Student grade:
---
correct or incorrect
---

Question:
---
I'm building a solar power installation and I need help \
working out the financials. 
- Land costs $100 / square foot
- I can buy solar panels for $250 / square foot
- I negotiated a contract for maintenance that will cost \
me a flat $100k per year, and an additional $10 / square \
foot
What is the total cost for the first year of operations \
as a function of the number of square feet.
--- 
Student's solution:
---
Let x be the size of the installation in square feet.
Costs:
1. Land cost: 100x
2. Solar panel cost: 250x
3. Maintenance cost: 100,000 + 100x
Total cost: 100x + 250x + 100,000 + 100x = 450x + 100,000
---
Actual solution:
"""
response = get_completion(prompt)
print(response)
```

### Model Limitations: Hallucinations

1.  Large language models may produce fabricated ideas that sound plausible but are not actually true, which are called hallucinations.
2.  The model may try to answer questions about obscure topics, even if it has not perfectly memorized the information it has seen during training.
3.  The model's boundary of knowledge may not be well-defined, leading to a higher likelihood of hallucinations.
4.  It is important to be aware of this limitation when developing applications with large language models.
5.  One way to reduce hallucinations is to ask the model to first find relevant quotes from the text before generating answers, as this can help trace the answer back to the source document.


## Iterative

-   The process of developing a prompt for large language models is iterative.
-   The first prompt is unlikely to work perfectly, but it's important to have a process to refine it.
-   The iterative process involves evaluating the output, identifying areas of improvement, and modifying the prompt accordingly.
-   Best practices for prompt development include being clear and specific, and if necessary, giving the model time to think.
-   Successful prompts are often arrived at through an iterative process.
-   For more sophisticated applications, prompts may need to be evaluated against a larger set of examples to drive incremental improvements.
-   Having a good process for developing prompts is more important than knowing the perfect prompt.

### Issue 1: The text is too long

Limit the number of words/sentences/characters.
Better not characters because LLM uses tokens and are bad a measure characters.

```
Use at most 50 words.
```

### Issue 2. Text focuses on the wrong details

Ask it to focus on the aspects that are relevant to the intended audience.

```
The description is intended for furniture retailers, 
so should be technical in nature and focus on the 
materials the product is constructed from.
```

### Issue 3. Description needs a table of dimensions

Ask it to extract information and organize it in a table.

```
After the description, include a table that gives the 
product's dimensions. The table should have two columns.
In the first column include the name of the dimension. 
In the second column include the measurements in inches only.

Give the table the title 'Product Dimensions'.

Format everything as HTML that can be used in a website. 
Place the description in a <div> element.
```

## Summarizing

Summarize with a word/sentence/character limit

```
Your task is to generate a short summary of a product
review from an ecommerce site. 

Summarize the review below, delimited by triple 
backticks, in at most 30 words. 

Review: ---{prod_review}---
```

Summarize with a focus on shipping and delivery

```
Summarize the review below, delimited by triple 
backticks, in at most 30 words, and focusing on any aspects
that mention shipping and delivery of the prod
```

Summarize with a focus on price and value

```
Summarize the review below, delimited by triple 
backticks, in at most 30 words, and focusing on any aspects
that are relevant to the price and perceived value. 
```

Try "extract" instead of "summarize"

```
From the review below, delimited by triple quotes
extract the information relevant to shipping and  
delivery. Limit to 30 words.
```

## Inferring

Sentiment (positive/negative)

```
What is the sentiment of the following product review, 
which is delimited with triple backticks?
```

```
What is the sentiment of the following product review, 
which is delimited with triple backticks?

Give your answer as a single word, either "positive" 
or "negative".
```

Identify types of emotions

```
Identify a list of emotions that the writer of the 
following review is expressing. Include no more than 
five items in the list. Format your answer as a list of 
lower-case words separated by commas.
```

Identify anger

```
Is the writer of the following review expressing anger?
The review is delimited with triple backticks.
Give your answer as either yes or no.
```

Doing multiple tasks at once

```
Identify the following items from the review text: 
- Sentiment (positive or negative)
- Is the reviewer expressing anger? (true or false)
- Item purchased by reviewer
- Company that made the item

The review is delimited with triple backticks. 
Format your response as a JSON object with 
"Sentiment", "Anger", "Item" and "Brand" as the keys.
If the information isn't present, use "unknown" 
as the value.
Make your response as short as possible.
Format the Anger value as a boolean.
```

## Transforming

ChatGPT can be used for various tasks such as translating text to different languages, transforming text to different tones, converting formats, spell/grammar checking, and proofreading/enhancing reviews.

### Translation

```
Translate the following English text to Spanish:
---Hi, I would like to order a blender---
```

### Tone Transformation

```
Translate the following from slang to a business letter: 
'Dude, This is Joe, check out this spec on this standing lamp.'
```

### Format Conversion

```python
data_json = { "resturant employees" :[ 
    {"name":"Shyam", "email":"shyamjaiswal@gmail.com"},
    {"name":"Bob", "email":"bob32@gmail.com"},
    {"name":"Jai", "email":"jai87@gmail.com"}
]}

prompt = f"""
Translate the following python dictionary from JSON to an HTML \
table with column headers and title: {data_json}
"""
response = get_completion(prompt)
print(response)
```

### Spellcheck/Grammar check

To signal to the LLM that you want it to proofread your text, you instruct the model to 'proofread' or 'proofread and correct'.

```
proofread and correct this review: ---{text}---
```

## Expanding

- Expanding is a task of generating longer pieces of text from short ones using a large language model.
- There are both positive and negative use cases of expanding, and it is important to use it responsibly.
- A language model can be used to generate personalized emails based on customer reviews by using prompts and extracting sentiment.
- It is important to be transparent when using AI-generated text and let the user know that it was generated by AI.
- The Chat Completions Endpoint format can be used to create custom chatbots using language models.

### Temperature

- The temperature parameter of a language model can be used to adjust the degree of exploration and randomness in its responses.
- A higher temperature leads to more varied and creative outputs, while a lower temperature results in more predictable responses.

![[Pasted image 20230503182141.png]]
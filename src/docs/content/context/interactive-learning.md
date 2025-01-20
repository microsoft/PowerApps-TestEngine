---
title: Interactive Learning
---

## The Goal of Interactive Documentation

The primary goal of interactive documentation is to create a living, breathing resource that evolves with the project and adapts to the needs of its users. Unlike static documentation, which can quickly become outdated and cumbersome, interactive documentation is designed to be:

- **Engaging**: Incorporates multimedia elements and interactive examples to enhance understanding.
- **User-Centric**: Tailored to meet the specific needs of different personas, ensuring that everyone from business stakeholders to technical experts can find the information they need.

## Interactive Assessments

To further enhance the value of interactive documentation, incorporating interactive assessments can be highly beneficial. These assessments can include conditional questions that adapt based on user responses, providing a personalized evaluation experience. 

### Conditional Questions

Conditional questions allow the assessment to branch based on the user's answers, ensuring that the questions remain relevant and targeted. For example:

1. **Question 1**: What is your primary role?
   - Business Professional
   - Architect
   - Technical Expert

2. **Question 2** (if Business Professional is selected): What aspect of the project are you most interested in?
   - Strategic Planning
   - Business Impact
   - Financial Analysis

3. **Question 2** (if Architect is selected): Which area of the system architecture do you focus on?
   - System Design
   - Integration
   - Security

4. **Question 2** (if Technical Expert is selected): What type of technical documentation do you need?
   - Code Examples
   - API References
   - Troubleshooting Guides

### Power Fx Expressions

To enable interactive evaluations, Power Fx expressions can be used. Power Fx is a powerful formula language for canvas apps that allows for dynamic and interactive content. Here are some examples of how Power Fx can be used in interactive documentation:

{% powerfx %}
Assert(1 = 1, "Unexpected value");
{% endpowerfx %}

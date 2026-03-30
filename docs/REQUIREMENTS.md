# Project Requirements

*This file will serve as a central reference for project requirements.*

1. Specifications
    - Functional specifications required for 
        - Website(s)
        - APIs
    - Wire Frames
        - Every screen to be included
        - Error states to be included
    - Style Guide
        - Bootstrap

2. Repository Requirements
    - Single shared repo
    - Pull requests
        - to merge any changes into `main`
        - need to be annotated with JIRA IDs
        - GitHub actions to execute unit tests

> [!IMPORTANT]
> **Make sure to annotate all PRs with JIRA IDs!**

3. Project Structure
    - 'Projects'
        - Website 
        - API
        - Unit Test
    - Can build and run in a container
    - Website and APIs have separate databases
    - Databases hosted in containers
    - Appropriate service abstractions (via interfaces)
    - Logging to debug via container console 

> [!IMPORTANT]
> **Logging MUST be implemented with various levels!**

4. Project Requirements
    - Front end
        - Supports user accounts
        - Uses basic authentication
    - API
        - Authentication using bearer tokens
        - Supports REST for CRUD operations
    - Front end web server invokes APIs

5. JIRA
    - User stories
        - Predefined user story format 
        - Acceptance criteria
        - Assigned to a team member
        - Reference project specifications and wire frames
    - Swim lanes
        - **Backlog**, **In Progress**, **In Test**, **Blocked*8, **Done**
        - Updated at stand ups

> [!IMPORTANT]
> **Make sure to do your daily stand ups!**

6. Two 2 Sprints
    - Ceremonies 
        - Sprint Kickoff 
        - Daily stand ups (**in Slack**)
        - Demo / Play tests
        - Sprint retrospectives

> [!TIP]
> **ALL of these are to be done in class WITH the exception of the daily stand ups.**

7. Project Themes 
    - Between website and API, must demonstrate themes and media that group spun for 

> [!IMPORTANT]
> **Our group is doing POKEMON LOCATIONS**

8. Website Frontend
    - Spec written in Markdown (`.md`)
    - Checked into GitHub
    - Wireframes
    - Boostrap for Styles
    - User create account and login

9. Websiite Backend
    - Spec written in Markdown (`.md`)
    - Checked into GitHub
    - Can be RPC
    - Must use a database in a container
    - Has unit tests for logic 
    - Requires basic authentication
    - Invokes the API project

10. API
    - Spec written in Markdown (`.md`)
    - Checked into GitHub
    - Must be RESTful
    - Must use a database in a container
    - Can be built and run inside of a container
    - Has unit tests for data validation
    - Requires a bearer token for auth 
    - Exposes a swagger view

11. GitHub
    - All merges to main go through a pull request
    - Unit tests for each project that run on merge and PR
    - PRs reference JIRA tickets

12. Process
    - User stories with acceptance criteria authored in Jira
    - Scrum rituals are being followed
    - Daily stand up notes in Slack

13. Future Phases
    - Integrate with another team's API
    - Monitoring and health checks
    - AB testing
    - Localization
    - TBD 



const API_BASE_URL = "http://localhost:8080";

async function clickCheckDatabaseConnection() {
    alert("Fetching.")
    const url = API_BASE_URL+"/health/db/";
    try {
        const response = await fetch(url);
        if(!response.ok) {
            alert("Error!");
            throw new Error(`Response status: ${response.status}`);
        }

        const result = await response.text();
        alert(result);
    } catch (error) {
        alert("Error!")
        console.error(error.message);
    }
}
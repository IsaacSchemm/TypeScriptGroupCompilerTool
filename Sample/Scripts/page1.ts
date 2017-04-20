function page1Start(): string {
    return `${onlyOnPage1()} - You are on page ${getPageId()} at time ${sharedFunction()} and the type of getPageId() is ${typeof getPageId()}`;
}
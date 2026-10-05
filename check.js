const fs = require('fs');
const path = require('path');

const SRC_DIR = './src';
const TESTS_DIR = './tests';

let violationsCount = 0;
const violationsLog = [];

function walkDir(dir, callback) {
    if (!fs.existsSync(dir)) return;
    const files = fs.readdirSync(dir);
    for (const file of files) {
        const fullPath = path.join(dir, file);
        const stat = fs.statSync(fullPath);
        if (stat.isDirectory()) {
            walkDir(fullPath, callback);
        } else if (file.endsWith('.cs')) {
            callback(fullPath, fs.readFileSync(fullPath, 'utf8'));
        }
    }
}

function walkCsproj(dir, callback) {
    if (!fs.existsSync(dir)) return;
    const files = fs.readdirSync(dir);
    for (const file of files) {
        const fullPath = path.join(dir, file);
        const stat = fs.statSync(fullPath);
        if (stat.isDirectory()) {
            walkCsproj(fullPath, callback);
        } else if (file.endsWith('.csproj')) {
            callback(fullPath, fs.readFileSync(fullPath, 'utf8'));
        }
    }
}

function report(filePath, message) {
    violationsCount++;
    violationsLog.push(`[${violationsCount}] ${filePath}: ${message}`);
}

function stripComments(content) {
    return content.replace(/\/\*[\s\S]*?\*\/|\/\/.*/g, '');
}

console.log('🔍 Запуск проверки конвенций проекта CarRental...\n');

walkCsproj(SRC_DIR, (filePath, content) => {
    const projectName = path.basename(filePath, '.csproj');

    if (projectName.includes('Domain')) {
        if (/ProjectReference[^>]*Include="[^"]*Application"/.test(content)) {
            report(filePath, 'КРИТИЧНО: Domain имеет ProjectReference на Application');
        }
        if (/ProjectReference[^>]*Include="[^"]*Infrastructure"/.test(content)) {
            report(filePath, 'КРИТИЧНО: Domain имеет ProjectReference на Infrastructure');
        }
        if (/ProjectReference[^>]*Include="[^"]*Api"/.test(content)) {
            report(filePath, 'КРИТИЧНО: Domain имеет ProjectReference на Api');
        }
    }
    if (projectName.includes('Application') && !projectName.includes('Domain')) {
        if (/ProjectReference[^>]*Include="[^"]*Infrastructure"/.test(content)) {
            report(filePath, 'КРИТИЧНО: Application имеет ProjectReference на Infrastructure');
        }
        if (/ProjectReference[^>]*Include="[^"]*Api"/.test(content)) {
            report(filePath, 'КРИТИЧНО: Application имеет ProjectReference на Api');
        }
    }
});

walkDir(path.join(SRC_DIR, 'CarRental.Domain'), (filePath, rawContent) => {
    const content = stripComments(rawContent);
    if (/using\s+CarRental\.Application\b/.test(content)) report(filePath, 'Domain не должен ссылаться на Application');
    if (/using\s+CarRental\.Infrastructure\b/.test(content)) report(filePath, 'Domain не должен ссылаться на Infrastructure');
    if (/using\s+CarRental\.Api\b/.test(content)) report(filePath, 'Domain не должен ссылаться на Api');
});

walkDir(path.join(SRC_DIR, 'CarRental.Application'), (filePath, rawContent) => {
    const content = stripComments(rawContent);
    if (/using\s+CarRental\.Infrastructure\b/.test(content)) report(filePath, 'Application не должен ссылаться на Infrastructure');
    if (/using\s+CarRental\.Api\b/.test(content)) report(filePath, 'Application не должен ссылаться на Api');
    if (/ApplicationDbContext/.test(content)) report(filePath, 'Application не должен использовать ApplicationDbContext напрямую');
    if (/using\s+Microsoft\.EntityFrameworkCore\b/.test(content)) {
        report(filePath, 'Application не должен напрямую использовать Microsoft.EntityFrameworkCore (используйте абстракции репозиториев)');
    }
});

walkDir(path.join(SRC_DIR, 'CarRental.Api', 'Controllers'), (filePath, rawContent) => {
    const content = stripComments(rawContent);

    if (/ApplicationDbContext/.test(content)) report(filePath, 'Контроллер не должен инжектить ApplicationDbContext');

    if (/new\s+\w+Repository\s*\(/.test(content)) report(filePath, 'Контроллер не должен создавать экземпляры репозиториев (new Repository)');

    if (/\bI\w+Repository\b/.test(content)) {
        report(filePath, 'Контроллер не должен внедрять (inject) интерфейсы репозиториев, используйте сервисы Application');
    }

    if (/using\s+CarRental\.Domain\.Entities\b/.test(content) || /CarRental\.Domain\.Entities\./.test(content)) {
        report(filePath, 'Контроллер не должен использовать Domain.Entities напрямую (используйте DTO)');
    }

    if (/catch\s*\(\s*Exception\s*\)/.test(content)) report(filePath, 'Запрещен общий catch (Exception) в контроллере');

    if (/throw new Exception\s*\(/.test(content)) report(filePath, 'Используйте кастомные исключения, а не throw new Exception()');

    if (/using\s+Microsoft\.EntityFrameworkCore\b/.test(content)) {
        report(filePath, 'Контроллер не должен импортировать Microsoft.EntityFrameworkCore');
    }
});

const appAndInfraDirs = [
    path.join(SRC_DIR, 'CarRental.Application'),
    path.join(SRC_DIR, 'CarRental.Infrastructure')
];

appAndInfraDirs.forEach(dir => {
    walkDir(dir, (filePath, rawContent) => {
        const content = stripComments(rawContent);

        if (!content.includes('Task<') && !/Task\s+/.test(content)) return;

        const methodRegex = /(?:public|protected|private|internal|async|\s)\s+(?:virtual|override|abstract\s+)?Task(?:<[^>]+>)?\s+([A-Za-z0-9_]+)\s*\(([^)]*)\)/g;
        let match;
        while ((match = methodRegex.exec(content)) !== null) {
            const methodName = match[1];
            const methodParams = match[2];

            if (['where', 'class', 'struct', 'interface'].includes(methodName)) continue;
            if (!methodName.endsWith('Async') && !['Task', 'Task.FromResult'].includes(methodName)) {
                report(filePath, `Асинхронный метод '${methodName}' должен иметь суффикс 'Async'`);
            }
            if (!methodParams.includes('CancellationToken')) {
                report(filePath, `Метод '${methodName}' возвращает Task, но в его параметрах отсутствует CancellationToken`);
            }
        }
    });
});

walkDir(path.join(SRC_DIR, 'CarRental.Application'), (filePath, rawContent) => {
    const content = stripComments(rawContent);

    const hasWriteOperations = /\.(Add|Update|Delete|Remove|AddAsync|UpdateAsync|RemoveAsync)\s*\(/.test(content);
    if (hasWriteOperations && !/SaveChangesAsync/.test(content)) {
        if (!/interface\s+\w+/.test(content)) {
            report(filePath, 'В файле производятся мутирующие операции (Add/Update/Delete), но не найден вызов SaveChangesAsync');
        }
    }
});

walkDir(SRC_DIR, (filePath, rawContent) => {
    const content = stripComments(rawContent);
    if (/throw new Exception\s*\(/.test(content)) {
        report(filePath, 'Запрещено использование throw new Exception(...) для бизнес-логики. Используйте кастомные исключения');
    }
});

const controllers = [];
walkDir(path.join(SRC_DIR, 'CarRental.Api', 'Controllers'), (filePath) => {
    const fileName = path.basename(filePath, '.cs');
    if (fileName !== 'BaseController') {
        controllers.push(fileName);
    }
});

const integrationTestFiles = [];
walkDir(path.join(TESTS_DIR, 'CarRental.IntegrationTests'), (filePath) => {
    integrationTestFiles.push(path.basename(filePath, '.cs'));
});

controllers.forEach(ctrl => {
    const hasIntegrationTest = integrationTestFiles.some(testFile => testFile.includes(ctrl));
    if (!hasIntegrationTest) {
        report(path.join(SRC_DIR, 'CarRental.Api', 'Controllers', `${ctrl}.cs`),
            `Для контроллера '${ctrl}' не найден соответствующий интеграционный тест в CarRental.IntegrationTests`);
    }
});

walkDir(path.join(TESTS_DIR, 'CarRental.UnitTests'), (filePath, rawContent) => {
    const content = stripComments(rawContent);
    if (/WebApplicationFactory|TestServer|HttpClient\s+CreateClient/.test(content)) {
        report(filePath, 'Интеграционные тесты (WebApplicationFactory/TestServer) не должны находиться в проекте UnitTests');
    }
});

if (violationsCount > 0) {
    console.log(`НАРУШЕНИЯ КОНВЕНЦИЙ НАЙДЕНЫ: ${violationsCount}\n`);
    violationsLog.forEach(log => console.log(log));
    process.exit(1);
} else {
    console.log('Нарушений архитектурных конвенций не найдено');
    process.exit(0);
}
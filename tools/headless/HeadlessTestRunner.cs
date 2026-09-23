// Minimal NUnit runner for the headless test pass (tools/run-tests-headless.sh).
//
// Why hand-rolled: Unity ships a patched nunit.framework and no console
// runner, and pulling a NuGet runner into a Unity repo adds a dependency
// for something the reflection loop below does in 40 lines. It looks for
// [Test] methods on [TestFixture] classes and reports pass/fail — which
// is all the CI gate needs to be useful.
using System;
using System.Reflection;

public static class HeadlessTestRunner
{
    public static int Main(string[] args)
    {
        // Two entry shapes: with an argument (the mono path runs the
        // runner against a separately compiled test assembly) and
        // without (dotnet run, where the tests are compiled into this
        // very assembly — so it inspects itself).
        Assembly assembly;
        if (args.Length > 0)
        {
            // Constrain the load to the compiled test assembly this runner
            // is FOR: tools/.headless-test/tests.dll, written by
            // run-tests-headless.sh. The argument is a build-system path,
            // never user input, but validating it costs nothing and keeps a
            // tainted-argument path out of the tooling entirely.
            string path = args[0];
            string scratch = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(
                    System.IO.Path.GetDirectoryName(
                        System.Reflection.Assembly.GetExecutingAssembly().Location)),
                ".headless-test");
            bool ok = !string.IsNullOrEmpty(path) &&
                path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) &&
                System.IO.File.Exists(path) &&
                System.IO.Path.GetFullPath(path).StartsWith(
                    System.IO.Path.GetFullPath(scratch),
                    StringComparison.OrdinalIgnoreCase);
            if (!ok)
            {
                Console.WriteLine("refusing to load '" + path +
                    "': expected the built test assembly under " + scratch);
                return 2;
            }
            try
            {
                assembly = Assembly.LoadFrom(path);
            }
            catch (Exception e)
            {
                Console.WriteLine("could not load " + path + ": " + e.Message);
                return 2;
            }
        }
        else
        {
            assembly = Assembly.GetExecutingAssembly();
        }

        int passed = 0, failed = 0, skipped = 0;
        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException e)
        {
            types = e.Types;
            Console.WriteLine("warning: some types failed to load");
        }

        foreach (Type type in types)
        {
            if (type == null || !type.IsClass || type.IsAbstract) continue;

            object[] fixtureAttrs = type.GetCustomAttributes(
                typeof(NUnit.Framework.TestFixtureAttribute), true);
            if (fixtureAttrs.Length == 0) continue;

            object instance = null;
            try
            {
                instance = Activator.CreateInstance(type);
            }
            catch (Exception e)
            {
                Console.WriteLine("FIXTURE-ERROR " + type.Name + " :: " + e.Message);
                failed++;
                continue;
            }

            foreach (MethodInfo method in type.GetMethods(
                BindingFlags.Public | BindingFlags.Instance))
            {
                object[] testAttrs = method.GetCustomAttributes(
                    typeof(NUnit.Framework.TestAttribute), true);
                if (testAttrs.Length == 0) continue;

                object[] ignoreAttrs = method.GetCustomAttributes(
                    typeof(NUnit.Framework.IgnoreAttribute), true);
                if (ignoreAttrs.Length > 0)
                {
                    skipped++;
                    Console.WriteLine("SKIP " + type.Name + "." + method.Name);
                    continue;
                }

                try
                {
                    method.Invoke(instance, null);
                    passed++;
                    Console.WriteLine("PASS " + type.Name + "." + method.Name);
                }
                catch (Exception e)
                {
                    failed++;
                    Exception inner = e.InnerException != null ? e.InnerException : e;
                    Console.WriteLine("FAIL " + type.Name + "." + method.Name +
                        "\n     " + inner.GetType().Name + ": " +
                        FirstLines(inner.Message, 3));
                }
            }
        }

        Console.WriteLine();
        Console.WriteLine("=== " + passed + " passed, " + failed + " failed, " +
            skipped + " skipped ===");
        return failed == 0 ? 0 : 1;
    }

    /// Test messages carry multi-line context; keep the log readable.
    static string FirstLines(string text, int count)
    {
        if (string.IsNullOrEmpty(text)) return "";
        string[] lines = text.Replace("\r", "").Split('\n');
        string result = "";
        for (int i = 0; i < lines.Length && i < count; i++)
            result += (i > 0 ? "\n     " : "") + lines[i];
        if (lines.Length > count) result += "\n     ...";
        return result;
    }
}

namespace BitFramework.Util
{
    public class Guard
    {
        public static void ParameterNotNull(object argumentValue, string message = null, )
    }

    class SomRef
    {
        public int x;
    }

    struct SomeVal
    {
        public int x;
    }

    class Program
    {
        static void ValueTypeDemo()
        {
            SomeVal v1 = new SomeVal(); //在栈上分配

            SomRef r1 = new SomRef(); //在堆上分配

            r1.x = 5; //指针指向托管堆修改

            v1.x = 5; //在栈上修改

            SomeVal v2 = v1; //在栈上分配并复制成员
            
            SomRef r2 = r1; //只复制引用（指针）
        }
    }
}
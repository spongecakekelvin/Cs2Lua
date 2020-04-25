# Cs2Lua
CSharp代码转lua，适用于使用lua实现热更新而又想有一个强类型检查的语言的场合



## 【源由】

1、基于unity3d的移动游戏开发，在android与ios平台上的限制不同。在android上，我们可以拆分可执行文件为多个dll，然后运行时动态加载除主程序外的其它dll，这样也就允许了对dll的单独更新，然而ios上此路不通，ios禁止使用jit与动态加载dll。为了实现热更新，游戏行业一般采用lua。

2、从语言角度比较，c#是强类型的编译型语言，而lua是解释型的动态脚本语言，在工程规模达到一定程度时，lua由于缺少编译时类型推导与检查，错误更多推迟到运行时暴露，这显然是一个弱点。这与web前端语言javascript有点类似。

3、近年来在web前端语言javascript领域出现了一些添加编译器类型检查的需求，2个例子是CoffeeScript与微软的TypeScript，这2种语言都给javascript添加了强类型与推导，但他们不是直接编译或解释执行自身，而是编译到javascript，这样保证与web的兼容。这个工程尝试类似的工作，不过我们不设计新的语言，而是直接基于C#语法，自动编译为lua。

4、vs2015的c#编译器开源工程Rosyln在编译器语法与语义信息方面提供了非常完善的API，这帮我们完成了从c#->lua编译的主要工作。

5、c#->lua实现后，可以考虑两种用法：

  a、以不同平台均运行lua，此时用c#编写程序主要利用编译器的类型检查与推导功能及c#语言的诸多适合架构大型工程的语言设施。
  
  b、在ios平台上运行lua，在android平台上直接将用于转lua的c#工程编译为dll并动态加载，这样在不同平台实现热更新的机制不同，运行效率不同，但开发时只需要进行一次开发。
 
 

## 【基本思路】

1、语法制导方式翻译到DSL（可以理解为简化了语法特性的中间语言），再由DSL经由生成器转换为lua。（c#语法、语义直接使用Rosyln工程）

2、C# class/struct -> lua table + metatable

3、inherit/property/event interface implementation -> metatable __index/__newindex

4、lambda/delegate/event -> 函数对象

5、generic -> 为每个generic实例生成一份代码，generic类本身不生成代码

6、interface -> 直接忽略

7、数组、集合 -> lua table

8、表达式 -> lua表达式 + 匿名函数调用

9、c#语句 -> lua语句 + 匿名函数调用



## 【比较复杂的转换】

1、ref/out参数【假设C#函数定义int f(int a, int b, ref int c, out int d)】 

  r = f(a,b,ref c,out d) 

=>  

  r,c,d = f(a,b,c)
  
2、带ref/out参数函数出现在表达式中  

  v1+v2+f(a,b,ref c,out d)   
  
=>   
  
  v1+v2+(function() local r; r,c,d = f(a,b,c); return r; end)()  
  
3、复合赋值  

  a+=f(a,b,ref c,out d)   

=>   

  a = a + (function() local r; r,c,d = f(a,b,c); return r; end)()
  
4、对象创建

  var o = new Class(a,b) { m_F = 123, m_F2 = 456 }   
  
=>   
  
  local o = (function() local obj; obj = Class(a,b); (function(self) self.m_F = 123; self.m_F2 = 456; end)(obj); return obj; end)() 
    
5、循环中的continue实现

  while(a<b)
  
  {
  
    ++a;
    
    if(a<10)
    
      continue; 
      
    if(a>100)
    
      break;
      
    ++a;
    
  }
  
=> 
  
  while a < b do 
  
    local isBreak = false; 
    
    repeat 
    
      a=a+1; 
      
      if a>2 then 
      
        isBreak=false; 
        
        break; 
        
      end;
      
      if a>100 then
      
        isBreak=true;
        
        break;
        
      end;
      
      a=a+1;
      
    until true;
    
    if isBreak then
    
      break;
      
    end;
    
  end;

6、switch中的break实现（与5类似，不需要引用额外变量）

  switch(cond)
  
  {
  
  case 1:
  
      if(a+b<12)
      
        break;
        
      a+=2;
      
      b+=4;
      
      c=a*b;
      
      break;
      
  default:
  
      c=123;
      
      break;
      
  case 2:
  
      c=456;
      
      break;
      
  }
  
=>
  
  if cond==1 then
  
    repeat
    
      if a+b<12 then
      
        break;
        
      end;
      
      a=a+2;
      
      b=b+4;
      
      c=a*b;
      
      break;
      
    until true;
    
  elseif cond==2 then
  
    repeat
    
      c=456;
      
      break;
      
    until true;
    
  else
  
    repeat
    
      c=123;
      
      break;
      
    until true;
    
  end;
  
  
  
## 【特殊处理】

1、转换出的lua代码不使用self表示对象自己，而是使用this表示对象自己，这样无需处理c#代码里用self作变量名的情形。类似的，转换出的lua使用base来表示父类子对象。类似的，property的get/set方法名也仍然是get/set，event接口实现的add/remove方法名也仍然使用add/remove。

2、泛型方法在转换时会将泛型参数当函数参数作用。

亦即

void GenericMethod<T>()

{

}

=>

GenericMethod = function(this, T)


end

***这种转换方法能适应unity3d的GetComponent方法

GetComponent<T>() => GetCompoent(Type)

3、为与Slua及dotnet reflection调用的机制一致，函数的out参数在调用时传入实参__cs2lua_out（使用Slua时此值为Slua.out否则为一空表）。



## 【lualib.lua】

*** cs2lua的实现假设C#导出给lua的API都采用slua。

Cs2Lua.exe负责按照c#语意选择合适的lua语法来实现对应语义，由于c#语言比lua复杂很多，在语言基础设施上很多是没法一一对应的，所以我们用lualib.lua来构建无法直接在lua语法层面简单实现的c#语义。主要包括：

1、基本运算

lua的运算符比c#少了很多，多出来的c#运算，有一些cs2lua经过语法变换对应到lua语句，比如复合赋值等；有一些cs2lua直接放弃，比如指针相关的操作；另一些则约定由lualib.lua提供一个对应的lua函数来实现，比如称位操作、位操作、条件表达式等。

2、对象语义

c#里的对象在lua里一般通过table+metatable来表示，与设计c#的对象运行时机制一样，我们需要在lua设计一套类似c#对象语义的运行时设施，这种机制也在lualib.lua里实现，cs2lua负责提供素材，比如method、property、field、event、indexer等对象的组成部分，组装成一个对象的工作则在lualib.lua里完成。

cs2lua将对象分类为被cs2lua转换的c#代码本身定义的对象与外部由slua导入的c#对象2大类，每类又分为普通对象、IList、IDictionary、ICollection这几种。

3、外部操作符重载处理

c#里的操作符重载比lua元表的操作符函数多，对lua元表支持的操作符重载，cs2lua按照slua的规则直接转换为lua对应操作（slua会在这些操作的元表函数里关联到实际的c#重载函数），不在lua元表里的操作符重载，需要在lualib.lua里处理，这个与slua的实现有关，本来c#的操作符重载函数都必须是类的静态方法，但slua在导出时将这些方法放到类实例上了，因此我们无法直接按c#操作符重载语义调用类的对应的静态操作符重载方法（对于被cs2lua转换的c#代码里定义的操作符重载，cs2lua在转换时采用c#的语义，所以可以直接转换为静态方法调用），而必须调用实例方法，由于操作符的实例有可能为空，不能直接写一个调用了事，判空的处理委托到lualib.lua里处理。

4、delegate的实现

cs2lua在转换delegate时委托到几个lua函数处理，这些函数在lualib.lua里实现，对被cs2lua转换的c#代码里定义的委托与slua导入的委托采用不同的函数，实现机制也不太相同。

5、外部indexer的实现

这块主要为了与slua的机制配合，放到lualib.lua里实现。

6、[]成员访问操作的实现

这个是预留，在语法上，对于数组访问[]与indexer已经分别独立处理，目前来看没发现其它种类的[]操作，但仍然预留了实现函数在lualib.lua里，由于这块不清楚对应的c#特性是什么，内部与外部实现都在lualib.lua里。

7、generic集合类型

cs2lua转换出的lua代码只使用lua的基础类型（不含string）与数组，其它类型假定都是经由slua导入。

由于lua不能直接对应generic语义，slua在导出c#类时要求导出的类必须是具体类，为了保证cs2lua在转换前后类名一致，所有generic类的实例类在导出时都需要进行一次继承，以保证导出的类在c#代码与lua代码里名字相同，比如导出List<int>，我们需要命名为IntList：

public class IntList : List<int>

{}

之后再由slua导出，之后在c#里就需要使用IntList而不是List<int>，这样才能保证转换出的lua代码能正确访问导出的类。

对于slua导入的API，这个约定没有问题，但被cs2lua转换的c#代码里也会有很频繁的需求使用常见的集合类型，因为被cs2lua转换的c#类转换后就是lua的table，天然可以支持动态类型，从这一角度出发，我们认为在被cs2lua转换的c#代码里使用的集合对象可以考虑转换为lua的table，借助table的元表机制，我们可以实现与c#里的集合对象相同的操作方法，这些代码都需要lua实现，所以放在lualib.lua里。

*** 需要注意的是，List<T>这类直接在被cs2lua转换的c#代码里使用的generic集合对象，由于转换为lua的table，不能作为参数传递给slua（除非修改slua的代码进行识别并转换，目前不采用这种思路）



## 【为什么不再支持各种lua的c#运行时？】

1、cs2lua需要对lua的c#封装进行较大修改，目前是基于slua的源码修改的，不能单独使用各类lua的c#运行时实现（比较大的修改是继承与重载方法的匹配机制）。

2、现在看来，cs2lua与lua的c#运行时在目标上有很大差异，手写lua更多是支持lua语言的特性，然后只需要允许c#提供lua api即可。自动翻译的lua实际上是使用lua来模拟c#语言的特性，此时需要更多向c#的习惯靠拢，而几乎所有lua的c#运行时机制在支持C#语言特性上都是残缺甚至很多特性都是不完备的。

3、尽管cs2lua已经限制了c#的很多语法，为了更完备的支持已经支持的c#语言特性（大概是c# 7.0除去本地方法与模式匹配语法，c#的语法糖对性能影响很大，放弃了很多语法糖特性），需要对lua的c#运行时进行大量修改以支持以c#开发时可以自由书写代码。


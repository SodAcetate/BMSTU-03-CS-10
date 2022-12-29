namespace Lab_10_1{
    class Program{


        public static async Task<PriceEntry[]> GetPrices(TickerEntry ticker){
                var url = $"https://query1.finance.yahoo.com/v7/finance/download/{ticker.Ticker}";
                var parameters = $"?period1={DateTimeOffset.Now.AddYears(-1).ToUnixTimeSeconds()}&period2={DateTimeOffset.Now.ToUnixTimeSeconds()}&interval=1d&events=history&includeAdjustedClose=true";

                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(url);

                // Здесь мы делаем запрос, пока не получим ответ
                HttpResponseMessage? response = null;
                while (response == null ){
                    
                    try{
                        response = await client.GetAsync(parameters);
                    }
                    catch(System.Net.Http.HttpRequestException){
                        Console.WriteLine($"{ticker.Ticker} : connection troubles, tryin' again");
                    }

                }
                
                // Считываем ответ как стрингу
                string rawResponse = await response.Content.ReadAsStringAsync();
                // Разделяем её на массив строк
                string[] data = rawResponse.Split('\n');
                
                // Переводим в массив цен
                PriceEntry[] result = new PriceEntry[0];

                foreach (string line in data){
                    // try, потому что не по всем акциям есть норм данные - в таком случае их не получится спарсить
                    try{
                        PriceEntry item = new PriceEntry{ TickerId = ticker.Id , Date = DateTime.Now };
                        // Пробуем делить строку запятыми
                        string[] dayData = line.Split(',');
                        // Берём среднюю цену за день
                        item.Price = ( Convert.ToDouble(dayData[2]) + Convert.ToDouble(dayData[3]) ) / 2;
                        // Парсим дату некрасиво
                        item.Date.AddYears(Convert.ToInt16(dayData[0].Split('-')[0]) - DateTime.Now.Year); 
                        item.Date.AddMonths(Convert.ToInt16(dayData[0].Split('-')[1]) - DateTime.Now.Month); 
                        item.Date.AddDays(Convert.ToInt16(dayData[0].Split('-')[2]) - DateTime.Now.Day); 
                        // Добавляем к массиву
                        result = result.Append(item).ToArray();
                    }
                    // Это исключение появляется, когда по акции вообще нет данных
                    catch(IndexOutOfRangeException){}
                    // Это исключение появляется для заголовочной строки и данных null
                    catch(FormatException){}


                }

                // Если смогли достать хоть какие-то данные, считаем среднее. Иначе - возвращаем null.
                return result;

            } 

        static void Main(string[] args){

            switch(args[0]){
                case "refresh":
                {
                    using (var CurrentDataBase = new TickersDbContext())
                    {
                        // Очищаем БАЗУ
                        CurrentDataBase.Tickers.RemoveRange(CurrentDataBase.Tickers);
                        CurrentDataBase.TodaysConditions.RemoveRange(CurrentDataBase.TodaysConditions);
                        CurrentDataBase.Prices.RemoveRange(CurrentDataBase.Prices);
                        CurrentDataBase.SaveChanges();
                        // Считываем имена тикеров в массив
                        string [] tickerArray = new string[0];
                        using (StreamReader tickerFileReader = new StreamReader("ticker.txt"))
                        {
                            tickerArray = tickerFileReader.ReadToEnd().Split('\n', StringSplitOptions.TrimEntries);
                        }
                        // Заполняем БАЗУ
                        // Для каждого имени тикера
                        foreach (string tickerName in tickerArray){
                            // Создаём соответствующий объект TickerEntry
                            TickerEntry ticker = new TickerEntry {Ticker = tickerName};
                            CurrentDataBase.Tickers.Add(ticker);
                            // Тут важно сохраниться шоб были айдишники
                            CurrentDataBase.SaveChanges();
                            // Спим чтобы не забанили
                            Thread.Sleep(150);
                            // Добываем цены на этот тикер и вносим в БАЗУ
                            PriceEntry[] priceArray = GetPrices(ticker).GetAwaiter().GetResult();
                            foreach (PriceEntry item in priceArray){
                                CurrentDataBase.Prices.Add(item);
                            }
                            // Тудейскондишн тоже ищем
                            TodaysConditionEntry todaysCondition = new TodaysConditionEntry{TickerId = ticker.Id};
                            // По массиву цен смотрим, насколько выросла/упала цена в последний день, вносим в БАЗУ
                            if (priceArray.Length > 1)
                                todaysCondition.State = priceArray[priceArray.Length-1].Price - priceArray[priceArray.Length-2].Price;
                            CurrentDataBase.TodaysConditions.Add(todaysCondition);
                        }
                        // Сохраняем БАЗУ
                        CurrentDataBase.SaveChanges();

                    }

                    break;
                }
                default:
                {
                    using (var CurrentDataBase = new TickersDbContext())
                    {
                        try{
                            // Ищем айди нужного тикера
                            int tickerId = CurrentDataBase.Tickers.FirstOrDefault(item => item.Ticker == args[0]).Id;
                            // Ищем и принтим тудейскондишн с нужным тикерайди
                            double tickerState = CurrentDataBase.TodaysConditions.FirstOrDefault(item => item.TickerId == tickerId).State;
                            Console.WriteLine($"{args[0]} : the price has gone {(tickerState>=0? "up" : "down")} ({Math.Round(tickerState*1000)/1000}$)");
                        }
                        catch { Console.WriteLine("No such ticker found"); }
                    }
                    break;
                }
            }
            
        }

    }

}
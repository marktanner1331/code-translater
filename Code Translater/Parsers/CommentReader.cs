using Code_Translater.AST;
using Code_Translater.Utilities;

namespace Code_Translater.Parsers
{
    public class CommentReader
    {
        private readonly Parser _parser;

        public CommentReader(Parser parser)
        {
            _parser = parser;
        }
        
        public Comment ReadComment()
        {
            _parser.TokenEnumerator.ReadRestOfLineRaw();

            Comment comment = new Comment
            {
                Value = _parser.TokenEnumerator.Value
            };

            _parser.TokenEnumerator.MoveNext();
            return comment;
        }
        
        public interface Parser
        {
            TokenEnumerator TokenEnumerator { get; }
        }
    }
}